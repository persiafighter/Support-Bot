﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Persiafighter.Applications.Support_Bot
{
    public class PastebinAnalyzerHelper
    {
        private List<string> Data;

        public PastebinAnalyzerHelper(string PastebinURI)
        {
            try
            {
                using (var wc = new WebClient())
                    Data = wc.DownloadString(WebRequest.CreateHttp(PastebinURI.StartsWith("https://pastebin.com/raw/") ? PastebinURI : "https://pastebin.com/raw/" + PastebinURI.TrimStart('h', 't', 'p', 's', ':').TrimStart('/').TrimStart('p', 'a', 's', 't', 'e', 'b', 'i', 'n', '.', 'c', 'o', 'm').TrimStart('/')).GetResponse().ResponseUri.ToString()).Split().ToList();
            }
            catch { }
        }

        public bool IsLogs()
        {
            try
            { 
                var r = new Regex("^\\d(\\d|(?<!/)/)*\\d$|^\\d$");
                return r.IsMatch(Data[0].TrimStart('[').TrimEnd(']'));
            }
            catch { return false; }
        }

        public async void WriteAndExplainErrors(SocketCommandContext context)
        {
            try
            {
                var r = new Regex("^\\d(\\d|(?<!/)/)*\\d$|^\\d$");
                List<string> errors = new List<string>();
                string CurrentError = "";
                foreach (string s in Data)
                {
                    if (CurrentError != string.Empty && r.IsMatch(s.TrimStart('[').TrimEnd(']')) && s.StartsWith("["))
                    {
                        errors.Add(CurrentError);
                        CurrentError = "";
                    }

                    if (s.ToLower() == "[error]" || s.ToLower() == "[exception]")
                        CurrentError += s + " ";
                    else if (CurrentError != string.Empty)
                        CurrentError += s + " ";
                }
                List<string> MSGS = new List<string>() { "Errors:\n" };
                foreach (string s in errors.Distinct().ToList())
                {
                    List<string> ss = s.Split().ToList();
                    if (s.Contains("Could not find dependency:"))
                        MSGS.Add("A library with name " + ss[5] + " is not found in your libraries folder for your server." + "\n");
                    else if (s.Contains("Invalid or outdated plugin assembly:"))
                        MSGS.Add("A library with name " + ss[6] + " is in your plugins directory. Rocket complains about it as it doesn't recognize it as a plugin." + "\n");
                    else if (s.Contains("Failed to generate an item with ID") || s.Contains("Failed to respawn an item with ID"))
                        MSGS.Add("The spawn tables in your server are attempting to spawn an item with ID " + ss[8] + ", but said item does not exist in either unturned or a workshop mod." + "\n");
                    else if (s.Contains("Error in MulticastDelegate PlayerDeath:"))
                        MSGS.Add("The plugin " + ss[32].Split('.')[0] + " had an issue in its code for when a player died. If this issue is common, please disable or remove the plugin as it is most likely broken" + "\n");
                    else if (s.Contains("MySql.Data.MySqlClient.MySqlException: Unable to connect to any of the specified MySQL hosts."))
                        MSGS.Add("The plugin " + ss[1] + " cannot connect to the MySQL server you specified. Make sure the mysql server exists and is online." + "\n");
                    else if (s.Contains("License key has not been setup"))
                        MSGS.Add("The plugin " + ss[1].TrimStart('[').TrimEnd(']') + " did not get it's license key set up. Please check the config and make sure the license is added." + "\n");
                    else if (s.Contains("Error: Cant save player info, plugin hasn't initialized properly.") || s.Contains("Error: Cant load player info from DB, plugin hasn't initialized properly."))
                        MSGS.Add("The plugin PlayerInfoLib tried tried to save or load player info, but it was not able to do this as it didn't load correctly.");
                    else
                        MSGS.Add("Unknown error: " + s + "\n");
                }
                List<string> lines = new List<string>();
                string currentLine = "";
                int maxLength = 2000;
                foreach (var currentWord in MSGS.Distinct())
                {
                    if ((currentLine.Length > maxLength) || ((currentLine.Length + currentWord.Length) > maxLength))
                    {
                        lines.Add(currentLine);
                        currentLine = "";
                    }

                    if (currentLine.Length > 0)
                    {
                        if (currentWord == "Keywords:")
                            currentLine += "\n\n" + currentWord;
                        else if (currentWord == "Message:")
                            currentLine += "\n" + currentWord;
                        else
                            currentLine += " " + currentWord;
                    }
                    else
                        currentLine += currentWord;

                    if (currentWord == MSGS.Distinct().Last())
                        lines.Add(currentLine);
                }
                foreach (string line in lines)
                    await context.Channel.SendMessageAsync(line);
            }
            catch (Exception ex)
            {
                await context.Channel.SendMessageAsync("An issue occured in my code (O.O): " + ex.Message);
            }
        }
    }
}
