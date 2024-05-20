using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TTRPGCreator.System;

namespace TTRPGCreator.Database
{
    public class DBEngine
    {
        private string connectionString = "Host=bb4b3ov4gawm6nymbdn7-postgresql.services.clever-cloud.com:50013;" +
                                          "Username=u9wlkygitnoqs10nh1k3;" +
                                          "Password=ebMlFm095MFyuO7PvLFAlQR9Ctdr3q;" +
                                          "Database=bb4b3ov4gawm6nymbdn7";
        #region Basic Functions

        public async Task<bool> Init()
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT * FROM data.server_games";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var serverId = reader.GetInt64(0);
                            var gameName = reader.GetString(1);

                            DataCache.gameList[(ulong)serverId] = gameName;

                            using (var innerConn = new NpgsqlConnection(connectionString))
                            {
                                await innerConn.OpenAsync();
                                using (var rulesCmd = new NpgsqlCommand($"SELECT * FROM {gameName}.ruleset", innerConn))
                                using (var rulesReader = await rulesCmd.ExecuteReaderAsync())
                                {
                                    while (await rulesReader.ReadAsync())
                                    {
                                        Ruleset ruleset = new Ruleset
                                        {
                                            diceRoll = rulesReader.GetString(0),
                                            statFormula = rulesReader.GetString(1)
                                        };

                                        DataCache.gameRules[gameName] = ruleset;
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> RunSQL(string query)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion

        #region Game Functions

        public async Task<List<string>> GetGames(ulong serverID)
        {
            try
            {
                var games = new List<string>();

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT substring(schema_name from position('_' in schema_name) + 1) AS game " +
                        "FROM information_schema.schemata " +
                        $"WHERE schema_name LIKE '{serverID}_%';";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            games.Add(reader.GetString(0));
                        }
                    }
                }

                return games;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public async Task<bool> SetGame(ulong serverID, string gameName)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO data.server_games (server_id, game_name) " +
                                   $"VALUES ({serverID}, '\"{serverID}_{gameName}\"') " +
                                   "ON CONFLICT (server_id) DO UPDATE SET game_name = EXCLUDED.game_name;";


                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = new NpgsqlCommand($"SELECT * FROM \"{serverID}_{gameName}\".ruleset", conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Ruleset ruleset = new Ruleset
                            {
                                diceRoll = reader.GetString(0),
                                statFormula = reader.GetString(1)
                            };

                            DataCache.gameRules[gameName] = ruleset;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> CreateGame(ulong serverID, string name, Ruleset ruleset)
        {
            try
            {
                string gameId = $"\"{serverID}_{name}\"";

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string statSetQuery = "";
                    statSetQuery += $"INSERT INTO {gameId}.statset (stat, name) " +
                                    $"VALUES";

                    foreach (var stat in ruleset.stats)
                        statSetQuery += $"('{stat.Item2}', '{stat.Item1}'),";
                    statSetQuery = statSetQuery.Substring(0, statSetQuery.Length - 1) + ";";

                    string statTableQuery = "";
                    foreach (var stat in ruleset.stats)
                        statTableQuery += $"{stat.Item2} INT,";
                    statTableQuery = statTableQuery.Substring(0, statTableQuery.Length - 1);

                    string rulesQuery = $"INSERT INTO {gameId}.ruleset (diceRoll, statFormula) " +
                                        $"VALUES ('{ruleset.diceRoll}', '{ruleset.statFormula}');";

                    await conn.OpenAsync();
                    string query = $"CREATE SCHEMA IF NOT EXISTS {gameId};" +
                                   $"CREATE TABLE IF NOT EXISTS {gameId}.statset (" +
                                       $"stat VARCHAR PRIMARY KEY," +
                                       $"name VARCHAR" +
                                       $");" +
                                   $"CREATE TABLE IF NOT EXISTS {gameId}.ruleset (" +
                                       $"diceRoll VARCHAR," +
                                       $"statformula VARCHAR" +
                                       $");" +
                                   $"CREATE TABLE IF NOT EXISTS {gameId}.statblocks (" +
                                       $"id SERIAL PRIMARY KEY," +
                                       statTableQuery +
                                       $");"
                                       ;

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = new NpgsqlCommand(statSetQuery, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = new NpgsqlCommand(rulesQuery, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Many tables

                    string tablesQuery = $"CREATE TABLE IF NOT EXISTS {gameId}.characters (" +
                                                           $"character_id SERIAL PRIMARY KEY," +
                                                           $"name VARCHAR," +
                                                           $"description TEXT," +
                                                           $"discord_id BIGINT" +
                                                           $");" +
                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.items (" +
                                                           $"item_id SERIAL PRIMARY KEY," +
                                                           $"name VARCHAR," +
                                                           $"description TEXT" +
                                                           $");" +
                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.statuses (" +
                                                           $"status_id SERIAL PRIMARY KEY," +
                                                           $"name VARCHAR," +
                                                           $"description TEXT," +
                                                           $"type VARCHAR" +
                                                           $");" +
                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.effects (" +
                                                           $"effect_id SERIAL PRIMARY KEY," +
                                                           $"effect VARCHAR" +
                                                           $");" +
                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.tags (" +
                                                           $"tag_id SERIAL PRIMARY KEY," +
                                                           $"tag VARCHAR UNIQUE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.effect_tags (" +
                                                           $"effect_id INT," +
                                                           $"tag_id INT," +
                                                           $"PRIMARY KEY (effect_id, tag_id)," +
                                                           $"FOREIGN KEY (effect_id) REFERENCES {gameId}.effects(effect_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (tag_id) REFERENCES {gameId}.tags(tag_id) ON DELETE CASCADE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.character_items (" +
                                                           $"character_id INT," +
                                                           $"item_id INT," +
                                                           $"quantity INT," +
                                                           $"equipped BOOLEAN," +
                                                           $"PRIMARY KEY (character_id, item_id)," +
                                                           $"FOREIGN KEY (character_id) REFERENCES {gameId}.characters(character_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (item_id) REFERENCES {gameId}.items(item_id) ON DELETE CASCADE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.character_statuses (" +
                                                           $"character_id INT," +
                                                           $"status_id INT," +
                                                           $"level INT," +
                                                           $"PRIMARY KEY (character_id, status_id)," +
                                                           $"FOREIGN KEY (character_id) REFERENCES {gameId}.characters(character_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id) ON DELETE CASCADE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.item_statuses (" +
                                                           $"item_id INT," +
                                                           $"status_id INT," +
                                                           $"level INT," +
                                                           $"PRIMARY KEY (item_id, status_id)," +
                                                           $"FOREIGN KEY (item_id) REFERENCES {gameId}.items(item_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id) ON DELETE CASCADE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.status_statuses (" +
                                                           $"parent_status_id INT," +
                                                           $"child_status_id INT," +
                                                           $"level INT," +
                                                           $"PRIMARY KEY (parent_status_id, child_status_id)," +
                                                           $"FOREIGN KEY (parent_status_id) REFERENCES {gameId}.statuses(status_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (child_status_id) REFERENCES {gameId}.statuses(status_id) ON DELETE CASCADE" +
                                                           $");" +

                                                       $"CREATE TABLE IF NOT EXISTS {gameId}.status_effects (" +
                                                           $"status_id INT," +
                                                           $"effect_id INT," +
                                                           $"level INT," +
                                                           $"PRIMARY KEY (status_id, effect_id)," +
                                                           $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id) ON DELETE CASCADE," +
                                                           $"FOREIGN KEY (effect_id) REFERENCES {gameId}.effects(effect_id) ON DELETE CASCADE" +
                                                           $");";

                    //string tablesQuery = $"CREATE TABLE IF NOT EXISTS {gameId}.characters (" +
                    //                   $"character_id SERIAL PRIMARY KEY," +
                    //                   $"name VARCHAR," +
                    //                   $"description TEXT," +
                    //                   $"discord_id BIGINT" +
                    //                   $");" +
                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.items (" +
                    //                   $"item_id SERIAL PRIMARY KEY," +
                    //                   $"name VARCHAR," +
                    //                   $"description TEXT" +
                    //                   $");" +
                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.statuses (" +
                    //                   $"status_id SERIAL PRIMARY KEY," +
                    //                   $"name VARCHAR," +
                    //                   $"description TEXT," +
                    //                   $"type VARCHAR" +
                    //                   $");" +
                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.effects (" +
                    //                   $"effect_id SERIAL PRIMARY KEY," +
                    //                   $"effect VARCHAR" +
                    //                   $");" +
                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.tags (" +
                    //                   $"tag_id SERIAL PRIMARY KEY," +
                    //                   $"tag VARCHAR UNIQUE" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.effect_tags (" +
                    //                   $"effect_id INT," +
                    //                   $"tag_id INT," +
                    //                   $"PRIMARY KEY (effect_id, tag_id)," +
                    //                   $"FOREIGN KEY (effect_id) REFERENCES {gameId}.effects(effect_id)," +
                    //                   $"FOREIGN KEY (tag_id) REFERENCES {gameId}.tags(tag_id)" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.character_items (" +
                    //                   $"character_id INT," +
                    //                   $"item_id INT," +
                    //                   $"quantity INT," +
                    //                   $"equipped BOOLEAN," +
                    //                   $"PRIMARY KEY (character_id, item_id)," +
                    //                   $"FOREIGN KEY (character_id) REFERENCES {gameId}.characters(character_id)," +
                    //                   $"FOREIGN KEY (item_id) REFERENCES {gameId}.items(item_id)" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.character_statuses (" +
                    //                   $"character_id INT," +
                    //                   $"status_id INT," +
                    //                   $"level INT," +
                    //                   $"PRIMARY KEY (character_id, status_id)," +
                    //                   $"FOREIGN KEY (character_id) REFERENCES {gameId}.characters(character_id)," +
                    //                   $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id)" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.item_statuses (" +
                    //                   $"item_id INT," +
                    //                   $"status_id INT," +
                    //                   $"level INT," +
                    //                   $"PRIMARY KEY (item_id, status_id)," +
                    //                   $"FOREIGN KEY (item_id) REFERENCES {gameId}.items(item_id)," +
                    //                   $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id)" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.status_statuses (" +
                    //                   $"parent_status_id INT," +
                    //                   $"child_status_id INT," +
                    //                   $"level INT," +
                    //                   $"PRIMARY KEY (parent_status_id, child_status_id)," +
                    //                   $"FOREIGN KEY (parent_status_id) REFERENCES {gameId}.statuses(status_id)," +
                    //                   $"FOREIGN KEY (child_status_id) REFERENCES {gameId}.statuses(status_id)" +
                    //                   $");" +

                    //               $"CREATE TABLE IF NOT EXISTS {gameId}.status_effects (" +
                    //                   $"status_id INT," +
                    //                   $"effect_id INT," +
                    //                   $"PRIMARY KEY (status_id, effect_id)," +
                    //                   $"FOREIGN KEY (status_id) REFERENCES {gameId}.statuses(status_id)," +
                    //                   $"FOREIGN KEY (effect_id) REFERENCES {gameId}.effects(effect_id)" +
                    //                   $");";

                    using (var cmd = new NpgsqlCommand(tablesQuery, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion

        #region Character Functions

        public async Task<List<Character>> GetCharacters(ulong serverID)
        {
            var characters = new List<Character>();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query = $"SELECT character_id, name, description, discord_id " +
                                   $"FROM {gameId}.characters";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var character = new Character
                            {
                                id = reader.GetInt64(0),
                                name = reader.GetString(1),
                                description = reader.GetString(2),
                                discord_id = reader.IsDBNull(3) ? (long?)null : reader.GetInt64(3)
                            };

                            characters.Add(character);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return characters;
        }

        public async Task<ulong?> GetCharacterDiscord(ulong serverID, ulong userID)
        {
            ulong? characterID = null;

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query = $"SELECT character_id " +
                                   $"FROM {gameId}.characters " +
                                   $"WHERE discord_id = {userID}";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            characterID = (ulong?)reader.GetInt64(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return characterID;
        }

        public async Task<bool> AddCharacter(ulong serverID, Character character)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query;
                    if (character.id != null)
                    {
                        query = $"INSERT INTO {gameId}.characters (character_id, name, description, discord_id) " +
                                $"VALUES (@id, @name, @description, @discordId) " +
                                $"ON CONFLICT (character_id) DO UPDATE SET " +
                                $"name = EXCLUDED.name, description = EXCLUDED.description, discord_id = EXCLUDED.discord_id;";
                    }
                    else
                    {
                        query = $"INSERT INTO {gameId}.characters (name, description, discord_id) " +
                                $"VALUES (@name, @description, @discord_id)";
                    }

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (character.id != null)
                        {
                            cmd.Parameters.AddWithValue("id", character.id);
                        }
                        cmd.Parameters.AddWithValue("name", character.name);
                        cmd.Parameters.AddWithValue("description", character.description);
                        cmd.Parameters.AddWithValue("discord_id", character.discord_id ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<int> AddCharacterItem(ulong serverID, long characterId, long itemId, int quantity, bool? equipped, bool delete)
        {
            try
            {
                Console.WriteLine(equipped?.ToString() ?? "null");

                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If delete is true, remove the row and return
                    if (delete)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.character_items WHERE character_id = @character_id AND item_id = @item_id";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@character_id", characterId);
                            deleteCmd.Parameters.AddWithValue("@item_id", itemId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        return 2; // Exit the function after deleting the row
                    }

                    // First, try to update the existing row
                    string query = $"UPDATE {gameId}.character_items SET " +
                                   $"quantity = quantity + @quantity, " +
                                   $"equipped = COALESCE(@equipped, equipped) " +
                                   $"WHERE character_id = @character_id AND item_id = @item_id " +
                                   $"RETURNING quantity;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@character_id", characterId);
                        cmd.Parameters.AddWithValue("@item_id", itemId);
                        cmd.Parameters.AddWithValue("@quantity", quantity);
                        cmd.Parameters.AddWithValue("@equipped", (object)equipped ?? DBNull.Value);

                        var totalQuantity = await cmd.ExecuteScalarAsync();

                        // If the row doesn't exist, totalQuantity will be null
                        if (totalQuantity == null)
                        {
                            // Insert a new row
                            query = $"INSERT INTO {gameId}.character_items (character_id, item_id, quantity, equipped) " +
                                    $"VALUES (@character_id, @item_id, @quantity, COALESCE(@equipped, false));";

                            cmd.CommandText = query;
                            await cmd.ExecuteNonQueryAsync();
                        }
                        else if ((int)totalQuantity <= 0)
                        {
                            // If the total quantity is less than or equal to 0, delete the row
                            query = $"DELETE FROM {gameId}.character_items WHERE character_id = @character_id AND item_id = @item_id";
                            cmd.CommandText = query;
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> AddCharacterStatus(ulong serverID, long characterId, long statusId, int level, bool delete)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If delete is true, remove the row and return
                    if (delete)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.character_statuses WHERE character_id = @character_id AND status_id = @status_id";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@character_id", characterId);
                            deleteCmd.Parameters.AddWithValue("@status_id", statusId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        return 2; // Exit the function after deleting the row
                    }

                    string query = $"INSERT INTO {gameId}.character_statuses (character_id, status_id, level) " +
                                   $"VALUES (@character_id, @status_id, @level) " +
                                   $"ON CONFLICT (character_id, status_id) DO UPDATE SET " +
                                   $"level = EXCLUDED.level;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@character_id", characterId);
                        cmd.Parameters.AddWithValue("@status_id", statusId);
                        cmd.Parameters.AddWithValue("@level", level);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<(bool, Character)> GetCharacter(ulong serverID, long characterId, bool full = false)
        {
            try
            {
                Character characterDetails = new Character();
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // Get character main information
                    using (var cmd = new NpgsqlCommand($"SELECT name, description, discord_id " +
                                                       $"FROM {gameId}.characters " +
                                                       $"WHERE character_id = @characterId", conn))
                    {
                        cmd.Parameters.AddWithValue("@characterId", characterId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                characterDetails.id = characterId;
                                characterDetails.name = reader.GetString(0);
                                characterDetails.description = reader.GetString(1);
                                if (!reader.IsDBNull(2))
                                    characterDetails.discord_id = reader.GetInt64(2);
                                else
                                    characterDetails.discord_id = null;
                            }
                        }
                    }

                    if (!full)
                        return (true, characterDetails);

                    // Get character items
                    characterDetails.items = new List<Item>();
                    using (var cmd = new NpgsqlCommand($"SELECT item_id, quantity, equipped " +
                                                       $"FROM {gameId}.character_items " +
                                                       $"WHERE character_id = @characterId", conn))
                    {
                        cmd.Parameters.AddWithValue("@characterId", characterId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long id = reader.GetInt64(0);
                                var querySuccess = await GetItem(serverID, id);
                                if (!querySuccess.Item1)
                                {
                                    Console.WriteLine("Get Item Failed");
                                    continue;
                                }
                                Item item = querySuccess.Item2;
                                item.id = reader.GetInt64(0);
                                item.quantity = reader.GetInt32(1);
                                item.equipped = reader.GetBoolean(2);

                                characterDetails.items.Add(item);
                            }
                        }
                    }

                    // Get character statuses
                    characterDetails.statuses = new List<Status>();
                    using (var cmd = new NpgsqlCommand($"SELECT status_id, level " +
                                                       $"FROM {gameId}.character_statuses " +
                                                       $"WHERE character_id = @characterId", conn))
                    {
                        cmd.Parameters.AddWithValue("@characterId", characterId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long id = reader.GetInt64(0);
                                var querySuccess = await GetStatus(serverID, id);
                                if (!querySuccess.Item1)
                                {
                                    Console.WriteLine("Get Status Failed");
                                    continue;
                                }
                                Status status = querySuccess.Item2;
                                status.id = reader.GetInt64(0);

                                characterDetails.statuses.Add(status);
                            }
                        }
                    }
                }

                return (true, characterDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, null);
            }
        }

        public async Task<bool> DeleteCharacter(ulong serverID, long characterId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand($"DELETE FROM {gameId}.characters WHERE character_id = @characterId", conn))
                    {
                        cmd.Parameters.AddWithValue("@characterId", characterId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion

        #region Item Functions

        public async Task<List<Item>> GetItems(ulong serverID)
        {
            var items = new List<Item>();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query = $"SELECT item_id, name, description " +
                                   $"FROM {gameId}.items";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new Item
                            {
                                id = reader.GetInt64(0),
                                name = reader.GetString(1),
                                description = reader.GetString(2)
                            };

                            items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return items;
        }

        public async Task<bool> AddItem(ulong serverID, Item item)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query;
                    if (item.id != null)
                    {
                        query = $"INSERT INTO {gameId}.items (item_id, name, description) " +
                                $"VALUES (@id, @name, @description) " +
                                $"ON CONFLICT (item_id) DO UPDATE SET " +
                                $"name = EXCLUDED.name, description = EXCLUDED.description;";
                    }
                    else
                    {
                        query = $"INSERT INTO {gameId}.items (name, description) " +
                                $"VALUES (@name, @description)";
                    }

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (item.id != null)
                        {
                            cmd.Parameters.AddWithValue("id", item.id);
                        }
                        cmd.Parameters.AddWithValue("name", item.name);
                        cmd.Parameters.AddWithValue("description", item.description);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<int> AddItemStatus(ulong serverID, long itemId, long statusId, int level, bool delete)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If delete is true, remove the row and return
                    if (delete)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.item_statuses WHERE item_id = @item_id AND status_id = @status_id";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@item_id", itemId);
                            deleteCmd.Parameters.AddWithValue("@status_id", statusId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        return 2; // Exit the function after deleting the row
                    }

                    string query = $"INSERT INTO {gameId}.item_statuses (item_id, status_id, level) " +
                                   $"VALUES (@item_id, @status_id, @level) " +
                                   $"ON CONFLICT (item_id, status_id) DO UPDATE SET " +
                                   $"level = EXCLUDED.level;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@item_id", itemId);
                        cmd.Parameters.AddWithValue("@status_id", statusId);
                        cmd.Parameters.AddWithValue("@level", level);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<(bool, Item)> GetItem(ulong serverID, long itemId, bool full = false)
        {
            try
            {
                Item itemDetails = new Item();
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // Get item main information
                    using (var cmd = new NpgsqlCommand($"SELECT name, description " +
                                                       $"FROM {gameId}.items " +
                                                       $"WHERE item_id = @itemId", conn))
                    {
                        cmd.Parameters.AddWithValue("@itemId", itemId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                itemDetails.name = reader.GetString(0);
                                itemDetails.description = reader.GetString(1);
                            }
                        }
                    }

                    if (!full)
                        return (true, itemDetails);

                    // Get item statuses
                    itemDetails.statuses = new List<Status>();
                    using (var cmd = new NpgsqlCommand($"SELECT status_id, level " +
                                                       $"FROM {gameId}.item_statuses " +
                                                       $"WHERE item_id = @itemId", conn))
                    {
                        cmd.Parameters.AddWithValue("@itemId", itemId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long id = reader.GetInt64(0);
                                var querySuccess = await GetStatus(serverID, id);
                                if (!querySuccess.Item1)
                                {
                                    Console.WriteLine("Get Status Failed");
                                    continue;
                                }
                                Status status = querySuccess.Item2;
                                status.id = reader.GetInt64(0);

                                itemDetails.statuses.Add(status);
                            }
                        }
                    }
                }


                return (true, itemDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, null);
            }
        }

        public async Task<bool> DeleteItem(ulong serverID, long itemId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand($"DELETE FROM {gameId}.items WHERE item_id = @itemId", conn))
                    {
                        cmd.Parameters.AddWithValue("@itemId", itemId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion

        #region Status Functions

        public async Task<List<Status>> GetStatuses(ulong serverID)
        {
            var statuses = new List<Status>();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query = $"SELECT status_id, name, description, type " +
                                   $"FROM {gameId}.statuses";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var status = new Status
                            {
                                id = reader.GetInt64(0),
                                name = reader.GetString(1),
                                description = reader.GetString(2),
                                type = reader.GetString(3)
                            };

                            statuses.Add(status);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return statuses;
        }

        public async Task<bool> AddStatus(ulong serverID, Status status)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query;
                    if (status.id != null)
                    {
                        query = $"INSERT INTO {gameId}.statuses (item_id, name, description, type) " +
                                $"VALUES (@id, @name, @description, @type) " +
                                $"ON CONFLICT (status_id) DO UPDATE SET " +
                                $"name = EXCLUDED.name, description = EXCLUDED.description, type = EXCLUDED.type;";
                    }
                    else
                    {
                        query = $"INSERT INTO {gameId}.statuses (name, description, type) " +
                                $"VALUES (@name, @description, @type)";
                    }

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (status.id != null)
                        {
                            cmd.Parameters.AddWithValue("id", status.id);
                        }
                        cmd.Parameters.AddWithValue("name", status.name);
                        cmd.Parameters.AddWithValue("description", status.description);
                        cmd.Parameters.AddWithValue("type", status.type);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<int> AddStatusStatus(ulong serverID, long parentStatusId, long childStatusId, int level, bool delete)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If delete is true, remove the row and return
                    if (delete)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.status_statuses WHERE parent_status_id = @parent_status_id AND child_status_id = @child_status_id";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@parent_status_id", parentStatusId);
                            deleteCmd.Parameters.AddWithValue("@child_status_id", childStatusId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        return 2; // Exit the function after deleting the row
                    }

                    string query = $"INSERT INTO {gameId}.status_statuses (parent_status_id, child_status_id, level) " +
                                   $"VALUES (@parent_status_id, @child_status_id, @level) " +
                                   $"ON CONFLICT (parent_status_id, child_status_id) DO UPDATE SET " +
                                   $"level = EXCLUDED.level;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@parent_status_id", parentStatusId);
                        cmd.Parameters.AddWithValue("@child_status_id", childStatusId);
                        cmd.Parameters.AddWithValue("@level", level);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<int> AddStatusEffect(ulong serverID, long statusId, long effectId, long level, bool delete)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If delete is true, remove the row and return
                    if (delete)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.status_effects WHERE status_id = @status_id AND effect_id = @effect_id";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@status_id", statusId);
                            deleteCmd.Parameters.AddWithValue("@effect_id", effectId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        return 2; // Exit the function after deleting the row
                    }

                    string query = $"INSERT INTO {gameId}.status_effects (status_id, effect_id, level) " +
                                   $"VALUES (@status_id, @effect_id, @level) " +
                                   $"ON CONFLICT (status_id, effect_id) " +
                                   $"DO UPDATE SET level = EXCLUDED.level;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status_id", statusId);
                        cmd.Parameters.AddWithValue("@effect_id", effectId);
                        cmd.Parameters.AddWithValue("@level", level);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public async Task<(bool, Status)> GetStatus(ulong serverID, long statusId, bool full = false)
        {
            try
            {
                Status statusDetails = new Status();
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // Get status main information
                    using (var cmd = new NpgsqlCommand($"SELECT name, description, type " +
                                                       $"FROM {gameId}.statuses " +
                                                       $"WHERE status_id = @statusId", conn))
                    {
                        cmd.Parameters.AddWithValue("@statusId", statusId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                statusDetails.name = reader.GetString(0);
                                statusDetails.description = reader.GetString(1);
                                statusDetails.type = reader.GetString(1);
                            }
                        }
                    }

                    if (!full)
                        return (true, statusDetails);

                    // Get status statuses
                    statusDetails.statuses = new List<Status>();
                    using (var cmd = new NpgsqlCommand($"SELECT child_status_id, level " +
                                                       $"FROM {gameId}.status_statuses " +
                                                       $"WHERE parent_status_id = @statusId", conn))
                    {
                        cmd.Parameters.AddWithValue("@statusId", statusId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long id = reader.GetInt64(0);
                                var querySuccess = await GetStatus(serverID, id);
                                if (!querySuccess.Item1)
                                {
                                    Console.WriteLine("Get Status Failed");
                                    continue;
                                }
                                Status status = querySuccess.Item2;
                                status.id = reader.GetInt64(0);

                                statusDetails.statuses.Add(status);
                            }
                        }
                    }

                    // Get status effects
                    statusDetails.effects = new List<Effect>();
                    using (var cmd = new NpgsqlCommand($"SELECT effect_id, level " +
                                                       $"FROM {gameId}.status_effects " +
                                                       $"WHERE status_id = @statusId", conn))
                    {
                        cmd.Parameters.AddWithValue("@statusId", statusId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long id = reader.GetInt64(0);
                                var querySuccess = await GetEffect(serverID, id, true);
                                if (!querySuccess.Item1)
                                {
                                    Console.WriteLine("Get Effect Failed");
                                    continue;
                                }
                                Effect effect = querySuccess.Item2;
                                effect.id = reader.GetInt64(0);
                                effect.level = reader.GetInt32(1);

                                statusDetails.effects.Add(effect);
                            }
                        }
                    }
                }

                return (true, statusDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, null);
            }
        }

        public async Task<bool> DeleteStatus(ulong serverID, long statusId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand($"DELETE FROM {gameId}.statuses WHERE status_id = @statusId", conn))
                    {
                        cmd.Parameters.AddWithValue("@statusId", statusId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion

        #region Effect Functions

        public async Task<List<Effect>> GetEffects(ulong serverID)
        {
            var effects = new List<Effect>();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query = $"SELECT effect_id, effect " +
                                   $"FROM {gameId}.effects";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var effect = new Effect
                            {
                                id = reader.GetInt64(0),
                                effect = reader.GetString(1)
                            };

                            effects.Add(effect);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return effects;
        }

        public async Task<bool> AddEffect(ulong serverID, Effect effect, List<string> tags = null)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];

                    await conn.OpenAsync();
                    string query;
                    if (effect.id != null)
                    {
                        query = $"INSERT INTO {gameId}.effects (effect_id, effect) " +
                                $"VALUES (@id, @effect) " +
                                $"ON CONFLICT (effect_id) DO UPDATE SET " +
                                $"effect = EXCLUDED.effect;";
                    }
                    else
                    {
                        query = $"INSERT INTO {gameId}.effects (effect) " +
                                $"VALUES (@effect) " +
                                $"RETURNING effect_id";
                    }

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        if (effect.id != null)
                        {
                            cmd.Parameters.AddWithValue("id", effect.id);
                        }
                        cmd.Parameters.AddWithValue("effect", effect.effect);

                        var id = await cmd.ExecuteScalarAsync();
                        if (id != null && id is long)
                        {
                            effect.id = (long)id;
                        }
                        else if (id != null && id is int) // In case the id is returned as an int
                        {
                            effect.id = (long)(int)id;
                        }
                    }
                }

                if(tags != null && tags.Count != 0)
                {
                    await AddEffectTags(serverID, (long)effect.id, tags, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> AddEffectTags(ulong serverID, long effectId, List<string> tags, bool clear)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // If clear is true, delete all existing tags for the effect
                    if (clear)
                    {
                        string deleteQuery = $"DELETE FROM {gameId}.effect_tags WHERE effect_id = @effectId;";
                        using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@effectId", effectId);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                    }

                    foreach (var tag in tags)
                    {
                        string query = $"INSERT INTO {gameId}.tags (tag) " +
                                       $"VALUES (@tag) " +
                                       $"ON CONFLICT (tag) DO NOTHING " +
                                       $"RETURNING tag_id;";

                        int tagId;
                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@tag", tag);

                            object result = await cmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                tagId = (int)result;
                            }
                            else
                            {
                                query = $"SELECT tag_id FROM {gameId}.tags WHERE tag = @tag;";
                                cmd.CommandText = query;
                                tagId = (int)await cmd.ExecuteScalarAsync();
                            }
                        }

                        query = $"INSERT INTO {gameId}.effect_tags (effect_id, tag_id) " +
                                $"VALUES (@effect_id, @tag_id) " +
                                $"ON CONFLICT (effect_id, tag_id) DO NOTHING;";

                        using (var cmd = new NpgsqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@effect_id", effectId);
                            cmd.Parameters.AddWithValue("@tag_id", tagId);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<List<string>> GetAllEffects(ulong serverID, long characterId, List<string> requiredTags)
        {
            List<string> effects = new List<string>();

            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    string query = $@"
                                    WITH RECURSIVE StatusHierarchy AS (
                                        SELECT character_statuses.status_id
                                        FROM {gameId}.character_statuses
                                        WHERE character_statuses.character_id = @characterId
                                        UNION
                                        SELECT item_statuses.status_id
                                        FROM {gameId}.character_items
                                        JOIN {gameId}.item_statuses ON character_items.item_id = item_statuses.item_id
                                        WHERE character_items.character_id = @characterId AND character_items.equipped = true
                                        UNION
                                        SELECT status_statuses.child_status_id
                                        FROM StatusHierarchy
                                        JOIN {gameId}.status_statuses ON StatusHierarchy.status_id = status_statuses.parent_status_id
                                    )
                                    SELECT effects.effect, status_effects.level
                                    FROM StatusHierarchy
                                    JOIN {gameId}.status_effects ON StatusHierarchy.status_id = status_effects.status_id
                                    JOIN {gameId}.effects ON status_effects.effect_id = effects.effect_id
                                    JOIN {gameId}.effect_tags ON effects.effect_id = effect_tags.effect_id
                                    JOIN {gameId}.tags ON effect_tags.tag_id = tags.tag_id
                                    WHERE tags.tag = ANY(@tags)
                                    GROUP BY effects.effect_id, status_effects.level
                                    HAVING COUNT(DISTINCT tags.tag) = @tagCount;";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@characterId", characterId);
                        cmd.Parameters.AddWithValue("@tags", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text, requiredTags.ToArray());
                        cmd.Parameters.AddWithValue("@tagCount", requiredTags.Count);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                long level = reader.GetInt64(1);
                                effects.Add(reader.GetString(0).Replace("|", $"({level.ToString()})"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

            return effects;
        }

        public async Task<(bool, Effect)> GetEffect(ulong serverID, long effectId, bool full)
        {
            try
            {
                Effect effectDetails = new Effect();
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    // Get effect main information
                    using (var cmd = new NpgsqlCommand($"SELECT effect " +
                                                       $"FROM {gameId}.effects " +
                                                       $"WHERE effect_id = @effectId", conn))
                    {
                        cmd.Parameters.AddWithValue("@effectId", effectId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                effectDetails.effect = reader.GetString(0);
                            }
                        }
                    }

                    if (!full)
                        return (true, effectDetails);

                    // Get effect tags
                    effectDetails.tags = new List<string>();

                    try
                    {
                        using (var cmd = new NpgsqlCommand($"SELECT tags.tag " +
                                                           $"FROM {gameId}.effect_tags, {gameId}.tags " +
                                                           $"WHERE effect_tags.tag_id = tags.tag_id AND effect_tags.effect_id = @effectId", conn))
                        {
                            cmd.Parameters.AddWithValue("@effectId", effectId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string tag = reader.GetString(0);
                                    effectDetails.tags.Add(tag);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                return (true, effectDetails);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return (false, null);
            }
        }

        public async Task<bool> DeleteEffect(ulong serverID, long effectId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(connectionString))
                {
                    string gameId = DataCache.gameList[serverID];
                    await conn.OpenAsync();

                    using (var cmd = new NpgsqlCommand($"DELETE FROM {gameId}.effects WHERE effect_id = @effectId", conn))
                    {
                        cmd.Parameters.AddWithValue("@effectId", effectId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        #endregion
    }
}
