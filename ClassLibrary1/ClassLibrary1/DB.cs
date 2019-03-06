using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using System;
using System.Collections.Generic;
namespace CDPlugin
{
    public static class DB
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;

        public static void Connect()
        {
            SQLEditor = new SqlTableEditor(TShock.DB,TShock.DB.GetSqlType()== SqlType.Sqlite?(IQueryBuilder)new SqliteQueryCreator():new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB,TShock.DB.GetSqlType() == SqlType.Sqlite?(IQueryBuilder)new SqliteQueryCreator():new MysqlQueryCreator());
            var table = new SqlTable("CDPlugin",
                new SqlColumn("userid", MySqlDbType.Int32) {Primary = true, Unique = true, Length = 6 },
                new SqlColumn("x", MySqlDbType.Float) { },
                new SqlColumn("y", MySqlDbType.Float) { },
                new SqlColumn("logout", MySqlDbType.Int32){ Length = 1 }
            );
            SQLWriter.EnsureTableStructure(table);
        }

        public static void Login(int _userid)
        {
            List<SqlValue> where = new List<SqlValue>();
            where.Add(new SqlValue("userid", _userid));
            var sql = SQLEditor.ReadColumn("CDPlugin", "userid", where);
            if (sql.Count > 0)
            {
                UpdateStatus(true, _userid);
            }
            else
                AddPos(new PlayerPos(_userid, 0, 0));
        }
        public static void Logout(int _userid, float _x, float _y)
        {
            PlayerPos retPos = new PlayerPos(_userid, _x, _y);
            UpdatePos(retPos, true);
        }

        public static void AddPos(PlayerPos info)
        {
            List<SqlValue> list = new List<SqlValue>();
            list.Add(new SqlValue("userid", info.userid));
            list.Add(new SqlValue("x", info.x));
            list.Add(new SqlValue("y", info.y));
            list.Add(new SqlValue("logout", Convert.ToInt32(false)));
            SQLEditor.InsertValues("CDPlugin", list);
        }
        public static void UpdateStatus(bool info, int _userid)
        {
            List<SqlValue> values = new List<SqlValue>();
            List<SqlValue> where = new List<SqlValue>();
            where.Add(new SqlValue("userid", _userid));
            values.Add(new SqlValue("logout", Convert.ToInt32(info)));
            SQLEditor.UpdateValues("CDPlugin", values, where);
        }
        public static void UpdatePos(PlayerPos info,bool _logout)
        {
            List<SqlValue> values = new List<SqlValue>();
            List<SqlValue> where = new List<SqlValue>();
            where.Add(new SqlValue("userid", info.userid));
            values.Add(new SqlValue("x", info.x));
            values.Add(new SqlValue("y", info.y));
            values.Add(new SqlValue("logout", Convert.ToInt32(_logout)));
            SQLEditor.UpdateValues("CDPlugin", values, where);
        }

        public static void DeletePos(int userid)
        {
            List<SqlValue> where = new List<SqlValue>();
            where.Add(new SqlValue("userid", userid));
            SQLWriter.DeleteRow("CDPlugin", where);
        }

        public static PlayerPos GetPos(int _userid)
        {
            PlayerPos retPos = new PlayerPos(_userid, 0, 0);
            List<SqlValue> where = new List<SqlValue>();
            where.Add(new SqlValue("userid", _userid));
            retPos.x = Convert.ToSingle (SQLEditor.ReadColumn("CDPlugin", "x", where)[0].ToString());
            retPos.y = Convert.ToSingle (SQLEditor.ReadColumn("CDPlugin", "y", where)[0].ToString());
            retPos.indatabase = Convert.ToBoolean( SQLEditor.ReadColumn("CDPlugin", "logout", where)[0]);
            return retPos;
        }
    }

    public class PlayerPos
    {
        public int userid;
        public float x;
        public float y;
        public bool indatabase;
        public PlayerPos(int _userid ,float _x, float _y)
        {
            userid = _userid;
            x = _x;
            y = _y;
            indatabase = false;
        }
    }
}
