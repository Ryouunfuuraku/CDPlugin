using System;
using System.IO;
using System.Data;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI.DB;
using Terraria.Localization;
using TShockAPI;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
namespace CDPlugin
{
    [ApiVersion(2, 1)]
    public class CDPlugin : TerrariaPlugin
    {
        private static IDbConnection db;
        public override string Author => "Ryouun";
        public override string Description => "Drop item when player die.";
        public override string Name => "CD";
        public override Version Version => new Version(1, 0, 0, 0);
        // 物品栏掉落的物品不销毁的概率
        private const int InventoryDDR = 60;
        // 物品栏掉落物品的概率
        private const int InventoryDR = 5;
        // 装备栏掉落的物品不销毁的概率
        private const int ArmDDR = 95;
        // 装备栏掉落物品的概率
        private const int ArmDR = 5;
        // 扩展栏掉落的物品不销毁的概率
        private const int MEDDR = 95;
        // 扩展栏掉落物品的概率
        private const int MEDR = 33;

        public CDPlugin(Main game) : base(game)
        {

        }
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, NetHooks_GetData);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnDBInitialize);
            
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, NetHooks_GetData);
            }
            base.Dispose(disposing);
        }

        private void NetHooks_GetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                args.Msg.reader.BaseStream.Position = args.Index;
                var playerId = args.Msg.whoAmI;
                var tPlayer = TShock.Players[playerId];
                var p = Main.player[playerId];
                var deathReason = "";
                using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    reader.ReadByte();
                    var reason = PlayerDeathReason.FromReader(reader);
                    deathReason = Convert.ToString(reason.GetDeathText(p.name));
                    if (!TShock.ServerSideCharacterConfig.Enabled)
                    {
                        return;
                    }
                    DDrop(Main.player[playerId], Main.player[playerId].inventory, 0, InventoryDDR, InventoryDR);
                    DDrop(Main.player[playerId], Main.player[playerId].armor, 59, ArmDDR, ArmDR);
                    DDrop(Main.player[playerId], Main.player[playerId].miscEquips, 89, MEDDR, MEDR);
                    
                }
            }
        }

        public void DDrop(Player p, Item[] inv, int os, int dontdestroyrate, int droprate)
        {
            for (var i = 0; i < inv.Length; i++)
            {
                if (Main.rand.Next(100) <= droprate)
                {
                    if (inv[i].Name != "")
                    {
                        DropItem(p, inv[i], os + i, dontdestroyrate);
                    }
                }
            }
        }

        void DropItem(Player p, Item i, int itemID, int ddrate)
        {
            if(Main.rand.Next(100) <= ddrate)
            {
                if (i.stack > 1)
                {
                    var id = Item.NewItem(p.position, i.width, i.height, i.type, (int)Math.Round(i.stack*Main.rand.Next(100)/100.0), true, i.prefix, true);
                }
                else
                {
                    var id = Item.NewItem(p.position, i.width, i.height, i.type, i.stack, true, i.prefix, true);
                }
            }
            i.stack = i.type = i.netID = 0;
            i.active = false;
            NetMessage.SendData(5, -1, -1, null, p.whoAmI, itemID, i.prefix);
        }

        private void OnDBInitialize(EventArgs args)
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] dbHost = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                            dbHost[0],
                            dbHost.Length == 1 ? "3306" : dbHost[1],
                            TShock.Config.MySqlDbName,
                            TShock.Config.MySqlUsername,
                            TShock.Config.MySqlPassword)

                    };
                    break;

                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "CDRPG.sqlite");
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;

            }

            SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlcreator.EnsureTableStructure(new SqlTable("CDRPG",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 4 },
                new SqlColumn("UID", MySqlDbType.Int32) { Length = 1 },
                new SqlColumn("Name", MySqlDbType.Text) { Length = 15 },
                new SqlColumn("Date", MySqlDbType.Text) { Length = 30 },
                new SqlColumn("Quote", MySqlDbType.Text) { Length = 100 }));
        }
    }
}
