using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;

namespace CDPlugin
{
    public static class DB
    {
        private static IDbConnection db;

        public static void Connect()
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
                    string sql = Path.Combine(TShock.SavePath, "CDPlugin.sqlite");
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;

            }

            SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            sqlcreator.EnsureTableStructure(new SqlTable("cdplugin",
                new SqlColumn("userid", MySqlDbType.Int32) { Primary = true, Unique = true, Length = 6 },
                new SqlColumn("x", MySqlDbType.Float) { },
                new SqlColumn("y", MySqlDbType.Float) { }));
        }

        public static void AddPos(PlayerPos info)
        {
            string query = $"INSERT INTO `cdplugin` (`userid`, `x`, `y`) VALUES ({info.userid}, '{info.x}', '{info.y}');";
            int result = db.Query(query);
            if (result != 1)
            {
                TShock.Log.ConsoleError("Error adding entry to database for user: " + info.userid);
            }
        }

        public static void UpdatePos(PlayerPos info)
        {
            string query = $"UPDATE `cdplugin` SET `x` = '{info.x}', `y` = '{info.y}' WHERE `userid` = {info.userid};";
            int result = db.Query(query);
            if (result != 1)
            {
                TShock.Log.ConsoleError("Error updating entry in database for user: " + info.userid);
            }
        }

        public static void DeletePos(int userid)
        {
            string query = $"DELETE FROM `cdplugin` WHERE `userid` = {userid}";
            int result = db.Query(query);
            if (result != 1)
            {
                TShock.Log.ConsoleError("Error deleting entry in database for user: " + userid);
            }
        }

        public static PlayerPos GetPos(TSPlayer plr)
        {
            PlayerPos retPos = new PlayerPos(plr, false);

            string query = $"SELECT * FROM `cdplugin` WHERE `userid` = {plr.User.ID};";
            using (var reader = db.QueryReader(query))
            {
                if (reader.Read())
                {
                    retPos.indatabase = true;
                    retPos.x = reader.Get<float>("x");
                    retPos.y = reader.Get<float>("y");
                }
                else
                {
                    retPos.indatabase = false;
                    retPos.x = plr.X;
                    retPos.y = plr.Y;
                    AddPos(retPos);
                }
            }
            return retPos;
        }


        public static void SetPos(TSPlayer plr)
        {
            //Using null to signify that it was not in database
            PlayerPos newInfo = new PlayerPos(plr, false);

            string query = $"SELECT * FROM `cdplugin` WHERE `userid` = {plr.User.ID};";
            using (var reader = db.QueryReader(query))
            {
                if (reader.Read())
                {
                    newInfo.indatabase = true;
                    newInfo.x = reader.Get<float>("x");
                    newInfo.y = reader.Get<float>("y");
                }
            }
        }
    }

    public class PlayerPos
    {
        public int userid;
        public float x;
        public float y;
        public bool indatabase;
        public PlayerPos(TSPlayer plr, bool _indatabase)
        {
            userid = plr.User.ID;
            x = plr.X;
            y = plr.Y;
            indatabase = _indatabase;
        }
    }
}
