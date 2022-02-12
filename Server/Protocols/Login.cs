﻿using System;
using System.IO;
using System.Text;

namespace Server.Protocols {
    class Login {
        public static void Handle(Client client) {
            switch(client.ReadByte()) {
                case 0x00_01: // 0059af3e // Auth
                    AcceptClient(client);
                    break;
                case 0x00_03: // 0059afd7 // after user selected world
                    SelectServer(client);
                    break;
                case 0x00_04: // 0059b08f // list of languages? sent after lobbyServer
                    ServerList(client);
                    break;
                case 0x00_0B: // 0059b14a // source location 0059b14a // sent after realmServer
                    Recieve_00_0B(client);
                    break;
                // case 0x00_10: break; // 0059b1ae // has something to do with T_LOADScreen // finished loading?
                case 0x00_63: // 0059b253
                    Ping(client);
                    break;

                default:
                    Console.WriteLine("Unknown");
                    break;
            }
        }

        #region Request
        // 00_01
        static void AcceptClient(Client client) {
            var data = PacketBuilder.DecodeCrazy(client.Reader);

            var userName = Encoding.ASCII.GetString(data, 1, data[0]);
            var password = Encoding.UTF7.GetString(data, 0x42, data[0x41]);

            client.Account = Program.database.GetPlayer(userName, password);

            if(client.Account == null) {
                SendInvalidLogin(client.Stream);
            } else {
                SendAcceptClient(client.Stream);
            }
        }

        // 00_03
        static void SelectServer(Client client) {
            int serverNum = client.ReadInt16();
            int worldNum = client.ReadInt16();

            // SendChangeServer(res);
            SendLobby(client, false);
        }

        // 00_04
        static void ServerList(Client client) {
            var count = client.ReadInt32();

            for(int i = 0; i < count; i++) {
                var len = client.ReadByte();
                var name = Encoding.ASCII.GetString(client.ReadBytes(len));
            }

            SendServerList(client.Stream);
        }

        // 00_0B
        static void Recieve_00_0B(Client client) {
            var idk1 = Encoding.ASCII.GetString(client.ReadBytes(client.ReadByte())); // "@"
            var idk2 = client.ReadInt32(); // = 0

            Send00_0C(client.Stream, 1);
            // SendCharacterData(res, false);
        }

        // 00_63
        static void Ping(Client client) {
            int number = client.ReadInt32();
            // Console.WriteLine($"Ping {number}");
        }
        #endregion

        #region Response
        // 00_01
        public static void SendLobby(Client client, bool lobby) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x1); // second switch

            b.AddString(lobby ? "LobbyServer" : "RealmServer", 1);

            b.WriteShort(0); // (*global_hko_client)->field_0xec
            b.WriteShort((short)client.Id);

            b.Send(client.Stream);
        }

        // 00_02_01
        static void SendAcceptClient(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x1); // third switch

            b.AddString("", 1);
            b.AddString("", 1); // appended to username??
            b.AddString("", 1); // blowfish encrypted stuff???

            b.Send(res);
        }

        // 00_02_02
        static void SendInvalidLogin(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x2); // third switch

            b.Send(res);
        }

        // 00_02_03
        static void SendPlayerBanned(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x2); // second switch
            b.WriteByte(0x3); // third switch

            b.AddString("01/01/1999", 1); // unban timeout (01/01/1999 = never)

            b.Send(res);
        }

        // 00_04
        static void SendServerList(Stream res) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x4); // second switch

            // some condition?
            b.WriteShort(0);

            // server count
            b.WriteInt(1);
            {
                b.WriteInt(1); // server number
                b.WriteWString("Test Sevrer");

                // world count
                b.WriteInt(1);
                {
                    b.WriteInt(1); // wolrd number
                    b.WriteWString("Test World");
                    b.WriteInt(0); // world status
                }
            }


            b.Send(res);
        }

        // 00_05
        static void Send00_05(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0x5); // second switch

            int count = 1;
            b.WriteInt(count);

            for(int i = 1; i <= count; i++) {
                b.WriteInt(i); // id??
                b.AddString("Test server", 1);
            }

            b.Send(clientStream);
        }

        // 00_0B
        static void SendChangeServer(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xB); // second switch

            b.WriteInt(1); // sets some global var

            // address of game server?
            b.AddString("127.0.0.1", 1); // address
            b.WriteShort(12345); // port

            b.Send(clientStream);
        }

        // 00_0C_x // x = 0-7
        static void Send00_0C(Stream clientStream, byte x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xC); // second switch

            b.WriteByte(x); // 0-7 switch

            b.Send(clientStream);
        }

        // 00_0D_x // x = 2-6
        static void Send00_0D(Stream clientStream, short x) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xD); // second switch

            b.WriteShort(x); // (2-6) switch

            b.Send(clientStream);
        }

        // 00_0E
        // almost the same as 00_0B
        static void Send00_0E(Stream clientStream) {
            var b = new PacketBuilder();

            b.WriteByte(0x0); // first switch
            b.WriteByte(0xE); // second switch

            b.WriteInt(0); // some global

            // parameters for FUN_0060699c
            b.AddString("127.0.0.1", 1);
            b.WriteShort(12345);

            b.Send(clientStream);
        }

        // 00_11
        public static void SendTimoutVal(Stream clientStream, int ms = 65536) {
            var b = new PacketBuilder();

            b.WriteByte(0x00); // first switch
            b.WriteByte(0x11); // second switch

            // sets some global timeout flag
            // if more ms have been passed since then game sends 0x7F and disconnects
            b.WriteInt(ms);

            b.Send(clientStream);
        }
        #endregion
    }
}