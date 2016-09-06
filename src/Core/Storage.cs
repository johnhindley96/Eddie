// <eddie_source_header>
// This file is part of Eddie/AirVPN software.
// Copyright (C)2014-2016 AirVPN (support@airvpn.org) / https://airvpn.org
//
// Eddie is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Eddie is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Eddie. If not, see <http://www.gnu.org/licenses/>.
// </eddie_source_header>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Eddie.Core
{
	public class Storage
    {
		public static string DataPath = "";
		public static bool Simulate = false; // If true, connections not really maded. Useful only during development of UI.

        public XmlElement Providers;

        private Dictionary<string, Option> m_Options = new Dictionary<string, Option>();
        
        public Storage()
        {

            EnsureDefaults();

            
            // Compute profile
			string profile = Get("profile");
			string path = Get("path");

			path = Platform.Instance.NormalizePath(path);

			if (path == "")
				path = Platform.Instance.GetDefaultDataPath();


            if (profile.IndexOf(".") != -1)
            {
                // Profile must not have an extension.
                profile = profile.Substring(0, profile.IndexOf("."));
				CommandLine.SystemEnvironment.Set("profile",profile);
            }

			if (Platform.Instance.IsPath(profile))            
            {
                // Is a path
				FileInfo fi = new FileInfo(Platform.Instance.NormalizePath(profile));
                DataPath = fi.DirectoryName;
                profile = fi.Name;
				CommandLine.SystemEnvironment.Set("profile",profile);

                if (TestDataPath(DataPath, true) == false)
                    DataPath = "";                
            }

            if (DataPath == "")
            {
                if (path == "home")
					path = Platform.Instance.GetUserFolder();
                else if (path == "program")
                    path = Platform.Instance.GetProgramFolder();

                if (path != "")
                {
                    DataPath = path;
                    if (TestDataPath(DataPath, true) == false)
                        DataPath = "";
                }
            }
                        
            if (DataPath == "")
            {
				DataPath = Platform.Instance.GetProgramFolder();
                if (Utils.HasAccessToWrite(DataPath) == false)
                    DataPath = "";
            }

            if (DataPath == "")
				DataPath = Platform.Instance.GetUserFolder();
        }

        public Dictionary<string, Option> Options
        {
            get
            {
                return m_Options;
            }
        }

        public static bool TestDataPath(string path, bool log)
        {
            if (Utils.HasAccessToWrite(path) == false)
            {
                if(log == true)
                    Engine.Instance.Logs.Log(LogType.Info, "Unable to write in path '" + path + "'");
                return false;
            }
            return true;
        }

        public string GetReportForSupport()
        {
            string result = "";
            foreach (Option option in Options.Values)
            {
                if (option.Important == false)
                    continue;
                
                if (option.Value != "")
                {
                    if (option.Value != option.Default)
                    {
                        string v = option.Value;
                        if (option.Code.IndexOf("password") != -1)
                            v = "(omissis)";
                        result += option.Code + ": " + v + "\n";
                    }
                }
            }
            return result;
        }

		public string GetMan(string format)
        {
            string body = "";
            foreach (Option option in Options.Values)
            {
                if (option.Man != "")
                {
                    body += "[option_block][option_code]" + option.Code + "[/option_code]\n\t\t" + option.Man.Replace("\n", "\n\t");
                    /*
                    if (manDefault != "")
                    {
                        if (option.Value != option.Default)
                            body += " - Current: '[i]" + option.Value + "[/i]'";
                        body += " - Default: " + ((option.Default == "") ? "-Empty-" : "'[i]" + option.Default + "[/i]'");
                        

                    }
                    */
                    if (option.Default != "")
                        body += " Default: [i]" + option.Default + "[/i]";
                    body += "[/option_block]\n";
                }
            }

            string o = "\n";
            o += "[sh]NAME[/sh]\n";
            o += "\t" + Messages.ManName.Replace("\n", "\n\t");
            o += "\n\n[sh]SYNOPSIS[/sh]\n";
            o += "\t" + Messages.ManSynopsis.Replace("\n", "\n\t");
            o += "\n\n[sh]DESCRIPTION[/sh]\n";
            o += "\t" + Messages.ManDescription.Replace("\n", "\n\t");
            o += "\n\n[sh]OPTIONS[/sh]\n";
            o += "\t" + Messages.ManHeaderOption1.Replace("\n", "\n\t");
            o += "\t" + Messages.ManHeaderOption2.Replace("\n", "\n\t");
            o += "\t" + Messages.ManHeaderOption3.Replace("\n", "\n\t");
            o += "\t" + Messages.ManHeaderOption4.Replace("\n", "\n\t");
            o += "\t[options_list]" + body.Replace("\n", "\n\t") + "[/options_list]";
            o += "\n\n[sh]COPYRIGHT[/sh]\n";
            o += "\t" + Messages.ManCopyright.Replace("\n", "\n\t\t");
            o += "\n";

            if (format == "man")
            {
                o = o.Replace("\n", "");

                // Header
                o = ".\\\"" + Messages.ManHeaderComment + "\n.TH airvpn 8 \"" + DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) + "\\n" + o;                

                o = o.Replace("[sh]", "\n.SH ");
                o = o.Replace("[/sh]", "\n");
                o = o.Replace("[link]", "\n.I ");
                o = o.Replace("[/link]", "\n");
                o = o.Replace("[option_code]", "\n.B \\-\\-");
                o = o.Replace("[/option_code]", "\n");
                o = o.Replace("[option_block]", "\n.TP\n");
                o = o.Replace("[/option_block]", "\n");
                o = o.Replace("[options_list]", "\n");
                o = o.Replace("[/options_list]", "\n");
                o = o.Replace("[i]", "\n.B ");
                o = o.Replace("[/i]", "\n");
                
                o = o.Replace("\t", "");

                // Normalization to avoid man layout break/issue
                for (;;)
                {
                    string orig = o;

                    // Remove empty line
                    o = o.Replace("\n\n", " ");
                    // Remove space
                    o = o.Replace("\n ", "\n");
                    // Remove space
                    o = o.Replace("\n ", "\n");

                    o = o.Trim();

                    if (o == orig)
                        break;
                }
            }
            else if (format == "bbcode")
            {
                o = o.Replace("[sh]", "[b]");
                o = o.Replace("[/sh]", "[/b]");
                o = o.Replace("[link]", "[url]");
                o = o.Replace("[/link]", "[/url]");
                o = o.Replace("[option_code]", "[b]");
                o = o.Replace("[/option_code]", "[/b]");
                o = o.Replace("[option_block]", "[*]");
                o = o.Replace("[/option_block]", "[/*]");
                o = o.Replace("[options_list]", "[list]");
                o = o.Replace("[/options_list]", "[/list]");
            }
            else if (format == "html")
            {
                o = o.Replace("[sh]", "<h3>");
                o = o.Replace("[/sh]", "</h3>");
                o = System.Text.RegularExpressions.Regex.Replace(o, "\\[link\\](.*?)\\[/link\\]", "<a href='$1'>$1</a>");                
                o = o.Replace("[option_code]", "<b>");
                o = o.Replace("[/option_code]", "</b>");
                o = o.Replace("[option_block]", "<li>");
                o = o.Replace("[/option_block]", "</li>");
                o = o.Replace("[options_list]", "<ul>");
                o = o.Replace("[/options_list]", "</ul>");
                o = o.Replace("[i]", "<i>");
                o = o.Replace("[/i]", "</i>");
            }
            else
            {
                // Text
                o = o.Replace("[sh]", "");
                o = o.Replace("[/sh]", "");
                o = o.Replace("[link]", "");
                o = o.Replace("[/link]", "");
                o = o.Replace("[option_code]", "");
                o = o.Replace("[/option_code]", "");
                o = o.Replace("[option_block]", "");
                o = o.Replace("[/option_block]", "");
                o = o.Replace("[options_list]", "");
                o = o.Replace("[/options_list]", "");
                o = o.Replace("[i]", "'");
                o = o.Replace("[/i]", "'");
            }
            
            return o;
        }

		public bool Exists(string name)
		{
            return m_Options.ContainsKey(name);
        }
                
        public string Get(string name)
        {
            lock (this)
            {
                if (CommandLine.SystemEnvironment.Exists(name))
                {
                    return CommandLine.SystemEnvironment.Get(name, "");
                }
                else if (Exists(name))
                {
                    Option option = Options[name];
                    if (option.Value != "")
                        return option.Value;
                    else
                        return option.Default;
                }
                else
                {
                    Engine.Instance.Logs.Log(LogType.Error, Messages.Format(Messages.OptionsUnknown, name));
                    return "";
                }
            }
        }

		public string GetLower(string name)
		{
			return Get(name).ToLowerInvariant();
		}

        public bool GetBool(string name)
        {
			return Conversions.ToBool(Get(name));            
        }

		public int GetInt(string name)
		{
			return Conversions.ToInt32(Get(name));
		}

        public float GetFloat(string name)
        {
            return Conversions.ToFloat(Get(name));
        }

        public Int64 GetInt64(string name)
        {
			return Conversions.ToInt64(Get(name));			
        }

        public List<string> GetList(string name)
        {
            List<string> output = new List<string>();
            string[] va = Get(name).Split(',');
            foreach (string v in va)
            {
                if(v != "")
                    output.Add(v);
            }
            return output;
        }

        public void Set(string name, string val)
        {
            lock (this)
            {
                if (Exists(name) == false)
                    Engine.Instance.Logs.Log(LogType.Warning, Messages.Format(Messages.OptionsUnknown, name));
                else
                    Options[name].Value = val;
            }
        }

        public void SetInt(string name, int val)
        {
            Set(name,val.ToString(CultureInfo.InvariantCulture));
        }

        public void SetFloat(string name, float val)
        {
            Set(name, val.ToString(CultureInfo.InvariantCulture));
        }

        public void SetBool(string name, bool val)
        {
            Set(name, val.ToString(CultureInfo.InvariantCulture));
        }

        public void SetList(string name, List<string> val)
        {
            Set(name, String.Join(",", val.ToArray()));
        }

        public void SetDefault(string name, string type, string val, string man)
        {
            SetDefault(name, type, val, man, true);
        }

        public void SetDefault(string name, string type, string val, string man, bool important)
        {
            Option option = new Option();
            option.Code = name;
            option.Type = type;
            option.Default = val;
            option.Man = man;
            option.Important = important;
            m_Options[option.Code] = option;
        }

		public void SetDefaultInt(string name, int val, string man)
        {
			SetDefault(name, "int", val.ToString(CultureInfo.InvariantCulture), man, true);
        }

		public void SetDefaultBool(string name, bool val, string man)
        {
			SetDefault(name, "bool", val.ToString(CultureInfo.InvariantCulture), man, true);
        }

        public void SetDefaultBool(string name, bool val, string man, bool important)
        {
            SetDefault(name, "bool", val.ToString(CultureInfo.InvariantCulture), man, important);
        }

        public void SetDefaultFloat(string name, float val, string man)
        {
            SetDefault(name, "float", val.ToString(CultureInfo.InvariantCulture), man, true);
        }

        public void Remove(string name)
        {
            lock (this)
            {
                m_Options.Remove(name);
            }
        }

        public string GetPath(string filename)
        {
			return DataPath + Platform.Instance.DirSep + filename;            
        }

        public void EnsureDefaults()
        {
			string NotInMan = ""; // Option not listed in 'man' documentation.

			SetDefaultBool("cli", false, Messages.ManOptionCli);
			SetDefaultBool("help", false, Messages.ManOptionHelp);
			SetDefault("help_format", "choice:text,bbcode,html,man", "text", Messages.ManOptionHelpFormat); // Maybe 'text' or 'bbcode' or 'html' or 'man'.
            SetDefaultBool("batch", false, NotInMan); // Don't lock interface, exit when connection is closed.
            SetDefault("login", "text", "", Messages.ManOptionLogin);
            SetDefault("password", "password", "", Messages.ManOptionPassword);
			SetDefaultBool("remember", false, Messages.ManOptionRemember);
            SetDefault("key", "text", "Default", Messages.ManOptionKey);
            SetDefault("server", "text", "", Messages.ManOptionServer);            
            SetDefaultBool("connect", false, Messages.ManOptionConnect);
			SetDefaultBool("netlock", false, Messages.ManOptionNetLock);

			SetDefault("profile", "text","AirVPN", Messages.ManOptionProfile); // Not in Settings
			SetDefault("path", "text", "", Messages.ManOptionPath); // Not in Settings // Path. Maybe a full path, or special values 'home' or 'program'.			
            
            SetDefault("servers.last", "text", "", NotInMan, false);
			SetDefault("servers.whitelist", "text", "", NotInMan, false); // Removed from man, because it's an hash list in >=2.11
			SetDefault("servers.blacklist", "text", "", NotInMan, false); // Removed from man, because it's an hash list in >=2.11
            SetDefaultBool("servers.startlast", false, Messages.ManOptionServersStartLast);
			SetDefaultBool("servers.locklast", false, Messages.ManOptionServersLockLast);
			SetDefault("servers.scoretype", "choice:Speed,Latency", "Speed", Messages.ManOptionServersScoreType);

			SetDefault("areas.whitelist", "text", "", Messages.ManOptionAreasWhiteList);
			SetDefault("areas.blacklist", "text", "", Messages.ManOptionAreasBlackList);

            SetDefault("discover.ip_webservice.list", "text", "https://ipleak.net/xml/{@ip};https://freegeoip.net/xml/{@ip};http://ip-api.com/xml/{@ip}", NotInMan);
            SetDefaultBool("discover.ip_webservice.first", true, NotInMan);

            SetDefaultBool("log.file.enabled", false, NotInMan);
			SetDefault("log.file.path", "text", "logs/eddie_%y-%m-%d.log", NotInMan);
			SetDefaultBool("log.level.debug", false, NotInMan);

			SetDefault("mode.protocol", "text", "AUTO", Messages.ManOptionModeProtocol);
			SetDefaultInt("mode.port", 443, Messages.ManOptionModePort);
			SetDefaultInt("mode.alt", 0, Messages.ManOptionModeAlt);
            
			SetDefault("proxy.mode", "text", "None", Messages.ManOptionProxyMode);
			SetDefault("proxy.host", "ip", "127.0.0.1", Messages.ManOptionProxyHost);
			SetDefaultInt("proxy.port", 8080, Messages.ManOptionProxyPort);
			SetDefault("proxy.auth", "text", "None", Messages.ManOptionProxyAuth);
			SetDefault("proxy.login", "text", "", Messages.ManOptionProxyLogin);
			SetDefault("proxy.password", "password", "", Messages.ManOptionProxyPassword);
            SetDefaultInt("proxy.tor.control.port", 9151, Messages.ManOptionProxyTorControlPort); 
            SetDefaultBool("proxy.tor.control.auth", true, Messages.ManOptionProxyTorControlAuth); 
            SetDefault("proxy.tor.control.password", "password", "", Messages.ManOptionProxyTorControlPassword); 

            SetDefault("routes.default", "choice:in,out", "in", Messages.ManOptionRoutesDefault);
			SetDefault("routes.custom", "text", "", Messages.ManOptionRoutesCustom);
			SetDefaultBool("routes.remove_default", false, Messages.ManOptionRoutesRemoveDefault); // Will be probably deprecated, issues with DHCP renew.

			SetDefault("dns.mode", "text", "auto", Messages.ManOptionDnsMode);            
			SetDefault("dns.servers", "text", "", Messages.ManOptionDnsServers);
			SetDefaultBool("dns.check", true, Messages.ManOptionDnsCheck);

			SetDefault("netlock.mode", "text", "auto", Messages.ManOptionNetLockMode);
			SetDefaultBool("netlock.allow_private", true, Messages.ManOptionNetLockAllowPrivate);
			SetDefaultBool("netlock.allow_ping", true, Messages.ManOptionNetLockAllowPing); 		
			SetDefault("netlock.allowed_ips", "text", "", Messages.ManOptionNetLockAllowedsIps); 

			SetDefault("ipv6.mode", "text", "disable", Messages.ManOptionIpV6);

			SetDefault("executables.openvpn", "path_file", "", Messages.ManOptionExecutablesOpenVpn);
			SetDefault("executables.ssh", "path_file", "", Messages.ManOptionExecutablesSsh);
			SetDefault("executables.ssl", "path_file", "", Messages.ManOptionExecutablesSsl);
			SetDefault("executables.curl", "path_file", "", Messages.ManOptionExecutablesCurl);

            SetDefault("openvpn.custom", "text", "", Messages.ManOptionOpenVpnCustom);
			SetDefault("openvpn.dev_node", "text", "", Messages.ManOptionOpenVpnDevNode);            
            SetDefaultInt("openvpn.sndbuf", -2, Messages.ManOptionOpenVpnSndBuf); // 2.11
            SetDefaultInt("openvpn.rcvbuf", -2, Messages.ManOptionOpenVpnRcvBuf); // 2.11
            SetDefault("openvpn.directives", "text", "client\r\ndev tun\r\nresolv-retry infinite\r\nnobind\r\npersist-key\r\npersist-tun\r\nverb 3\r\nconnect-retry-max 1\r\nping 10\r\nping-exit 32\r\nexplicit-exit-notify 5", Messages.ManOptionOpenVpnDirectives);
            SetDefaultBool("openvpn.skip_defaults", false, Messages.ManOptionOpenVpnSkipDefaults);
            
			// Not in Settings
			SetDefaultInt("openvpn.management_port", 3100, Messages.ManOptionOpenVpnManagementPort);
			SetDefaultInt("ssh.port", 0, Messages.ManOptionSshPort); 
			SetDefaultInt("ssl.port", 0, Messages.ManOptionSslPort);

            SetDefaultBool("os.single_instance", true, Messages.ManOptionOsSingleInstance);

            if (WebServer.GetPath() != "")
            {                
                SetDefaultBool("webui.enabled", true, Messages.ManOptionWebUiEnabled);
                SetDefault("webui.ip", "text", "localhost", Messages.ManOptionWebUiAddress);
                SetDefaultInt("webui.port", 4649, Messages.ManOptionWebUiPort);
            }

            SetDefaultBool("advanced.expert", false, Messages.ManOptionAdvancedExpert);			
			SetDefaultBool("advanced.check.route", true, Messages.ManOptionAdvancedCheckRoute);
			
			SetDefaultInt("advanced.penality_on_error", 30, NotInMan);

			SetDefaultBool("pinger.enabled", true, Messages.ManOptionAdvancedPingerEnabled);
			SetDefaultInt("pinger.delay", 0, Messages.ManOptionAdvancedPingerDelay);
			SetDefaultInt("pinger.retry", 0, Messages.ManOptionAdvancedPingerRetry);
			SetDefaultInt("pinger.jobs", 10, Messages.ManOptionAdvancedPingerJobs);
			SetDefaultInt("pinger.valid", 0, Messages.ManOptionAdvancedPingerValid);

			SetDefaultInt("advanced.manifest.refresh", -1, NotInMan);
			            
            SetDefaultBool("advanced.skip_privileges", false, NotInMan); // Skip 'root' detection.
            SetDefaultBool("advanced.skip_tun_detect", false, NotInMan); // Skip TUN driver detection.
            SetDefaultBool("advanced.skip_alreadyrun", false, NotInMan); // Continue even if openvpn is already running.     
            SetDefaultBool("servers.allow_anyway", false, NotInMan); // Allow connection to server in 'Closed' status            
            SetDefaultBool("advanced.testonly", false, NotInMan); // Disconnect when connection occur.


            EnsureDefaultsEvent("app.start");
            EnsureDefaultsEvent("app.stop");
			EnsureDefaultsEvent("session.start");
			EnsureDefaultsEvent("session.stop");
            EnsureDefaultsEvent("vpn.pre");
            EnsureDefaultsEvent("vpn.up");
            EnsureDefaultsEvent("vpn.down");

			// Windows only			
			SetDefault("windows.adapter_service", "text", "tap0901", Messages.ManOptionWindowsAdapterService);
			SetDefaultBool("windows.disable_driver_upgrade", false, Messages.ManOptionWindowsDisableDriverUpgrade);
            SetDefaultBool("windows.tap_up", true, Messages.ManOptionWindowsTapUp);
            SetDefaultBool("windows.dhcp_disable", false, Messages.ManOptionWindowsDhcpDisable);
            SetDefaultBool("windows.wfp", true, Messages.ManOptionWindowsWfp); 
            SetDefaultBool("windows.wfp.dynamic", false, Messages.ManOptionWindowsWfpDynamic);
            SetDefaultBool("windows.ipv6.os_disable", false, Messages.ManOptionWindowsIPv6DisableAtOs); // Must be default FALSE if WFP works well
            SetDefaultBool("windows.dns.force_all_interfaces", true, Messages.ManOptionWindowsDnsForceAllInterfaces); // Important: With WFP can be false, but users report DNS leak. Maybe not a real DNS Leak, simply request on DNS of other interfaces through VPN tunnel.
            SetDefaultBool("windows.dns.lock", true, Messages.ManOptionWindowsDnsLock);

            // General UI
            SetDefault("ui.unit", "text", "", Messages.ManOptionUiUnit);

            // GUI only            
            SetDefaultBool("gui.exit_confirm", true, NotInMan, false);
			SetDefault("gui.skin", "text", "Light", NotInMan, false);
			SetDefaultBool("gui.tos", false, NotInMan, false);
            SetDefault("gui.font.normal.name", "text", "", NotInMan);
            SetDefaultFloat("gui.font.normal.size", 0, NotInMan);
			SetDefaultInt("gui.log_limit", 1000, NotInMan);
			SetDefault("gui.window.main", "text", "", NotInMan, false);
            SetDefault("gui.list.servers", "text", "", NotInMan, false);
            SetDefault("gui.list.areas", "text", "", NotInMan, false);
            SetDefault("gui.list.logs", "text", "", NotInMan, false);

            // GUI - Windows only
            SetDefaultBool("gui.windows.start_minimized", false, NotInMan);
            SetDefaultBool("gui.windows.tray", true, NotInMan);
			SetDefaultBool("gui.windows.notifications", true, NotInMan);

			// GUI - OSX Only
			SetDefaultBool("gui.osx.notifications", false, NotInMan);
			// SetDefaultBool("gui.osx.dock", false, NotInMan); // See this FAQ: https://airvpn.org/topic/13331-its-possible-to-hide-the-icon-in-dock-bar-under-os-x/
			SetDefaultBool("gui.osx.visible", false, NotInMan);
			SetDefault ("gui.osx.style", "text", "light", NotInMan);
			SetDefaultBool("gui.osx.sysbar.show_info", false, NotInMan);
            SetDefaultBool("gui.osx.sysbar.show_speed", false, NotInMan);
            SetDefaultBool("gui.osx.sysbar.show_server", false, NotInMan);

            // TODO: we need to test params with space in different linux platform, with focus on escaping gksu/kdesu shell to obtain elevated privileges
            SetDefault("paramtest", "text", "", NotInMan, false);



            // Command-line only?
            Options["cli"].CommandLineOnly = true;
            Options["help"].CommandLineOnly = true;
            Options["help_format"].CommandLineOnly = true;

            // Internal only?
            Options["gui.window.main"].InternalOnly = true;
            Options["gui.list.servers"].InternalOnly = true;
            Options["gui.list.areas"].InternalOnly = true;
            Options["gui.list.logs"].InternalOnly = true;

            // Don't clean with user Reset All
            Options["login"].DontUserReset = true;
            Options["password"].DontUserReset = true;
            Options["remember"].DontUserReset = true;
            Options["key"].DontUserReset = true;
        }

        public void EnsureDefaultsEvent(string name)
        {
			SetDefault("event." + name + ".filename", "path_file", "", Messages.ManOptionEventFileName);
			SetDefault("event." + name + ".arguments", "text", "", Messages.ManOptionEventArguments);
			SetDefaultBool("event." + name + ".waitend", true, Messages.ManOptionEventWaitEnd);
        }

        public void ResetAll(bool force)
        {
            foreach (Option option in Options.Values)
            {
                if( (force == false) && (option.DontUserReset) )
                        continue;
                
                option.Value = "";
            }
        }

        public void Save()
        {
			string path = GetPath(Get("profile") + ".xml");

			bool remember = GetBool("remember");
			
            lock (this)
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

                XmlElement rootNode = xmlDoc.CreateElement("eddie");
                xmlDoc.InsertBefore(xmlDeclaration, xmlDoc.DocumentElement);

                XmlElement optionsNode = xmlDoc.CreateElement("options");
                rootNode.AppendChild(optionsNode);

                xmlDoc.AppendChild(rootNode);

                foreach (Option option in Options.Values)
                {
                    bool skip = false;

                    if ((remember == false) && (option.Code == "login"))
                        skip = true;
                    if ((remember == false) && (option.Code == "password"))
                        skip = true;

                    if (option.CommandLineOnly)
                        skip = true;

                    if ((option.Value == "") || (option.Value == option.Default))
                        skip = true;

                    if (skip == false)
                    {
                        XmlElement itemNode = xmlDoc.CreateElement("option");
                        itemNode.SetAttribute("name", option.Code);
                        itemNode.SetAttribute("value", option.Value);
                        optionsNode.AppendChild(itemNode);
                    }
                }


                XmlElement providersNode = xmlDoc.CreateElement("providers");
                rootNode.AppendChild(providersNode);
                foreach (Provider provider in Engine.Instance.ProvidersManager.Providers)
                {
                    XmlNode providerNode = xmlDoc.ImportNode(provider.Storage.DocumentElement, true);
                    providersNode.AppendChild(providerNode);
                }

                if (Engine.Instance.ProvidersManager.GetProvidersPath() != "")
                {
                    // Move providers->AirVPN to root.
                    XmlElement xmlAirVPN = Utils.XmlGetFirstElementByTagName(providersNode, "AirVPN");
                    if (xmlAirVPN != null)
                    {
                        foreach (XmlElement xmlChild in xmlAirVPN.ChildNodes)
                            Utils.XmlCopyElement(xmlChild, xmlDoc.DocumentElement);
                        providersNode.RemoveChild(xmlAirVPN);
                    }
                    if (providersNode.ChildNodes.Count == 0)
                        providersNode.ParentNode.RemoveChild(providersNode);
                }

                xmlDoc.Save(path);
            }

			if (Platform.Instance.IsUnixSystem())
				Platform.Instance.ShellCmd("chmod 600 \"" + path + "\"");
			
        }

        public void Load()
        {
            lock (this)
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();

                    Providers = xmlDoc.CreateElement("providers");

                    string profile = Get("profile");
					if (profile.ToLowerInvariant() == "none")
						return;

					string Path = GetPath(profile + ".xml");

					Engine.Instance.Logs.Log(LogType.Verbose, Messages.Format(Messages.OptionsRead, Path));

					if (File.Exists(Path) == false)
					{
						Engine.Instance.Logs.Log(LogType.Verbose, Messages.OptionsNotFound);
						return;
					}

                    xmlDoc.Load(Path);

                    ResetAll(true);

                    Providers = Utils.XmlGetFirstElementByTagName(xmlDoc.DocumentElement, "providers");
                    if(Providers == null)
                        Providers = xmlDoc.CreateElement("providers");

                    XmlNode nodeOptions = xmlDoc.DocumentElement.GetElementsByTagName("options")[0];
                    Dictionary<string, string> options = new Dictionary<string, string>();
                    foreach (XmlElement e in nodeOptions)
                    {
                        string name = e.Attributes["name"].Value;
                        string value = e.Attributes["value"].Value;

                        CompatibilityManager.FixOption(ref name, ref value);

                        options[name] = value;
                    }

                    CompatibilityManager.FixOptions(options);
                    foreach (KeyValuePair<string, string> item in options)
                        Set(item.Key, item.Value);                        

                    // For compatibility <3
                    XmlElement xmlManifest = Utils.XmlGetFirstElementByTagName(xmlDoc.DocumentElement, "manifest");
                    if (xmlManifest != null)
                    {
                        XmlElement providerAirVpn = xmlDoc.CreateElement("AirVPN");
                        Providers.AppendChild(providerAirVpn);

                        Utils.XmlCopyElement(xmlManifest, providerAirVpn);

                        XmlElement xmlUser = Utils.XmlGetFirstElementByTagName(xmlDoc.DocumentElement, "user");
                        if (xmlUser != null) // Compatibility with old manifest < 2.11
                        {
                            XmlElement oldKeyFormat = xmlUser.SelectSingleNode("keys/key[@id='default']") as XmlElement;
                            if (oldKeyFormat != null)
                            {
                                oldKeyFormat.SetAttribute("name", "Default");
                            }
                        }
                        if (xmlUser != null)
                            Utils.XmlCopyElement(xmlUser, providerAirVpn);                        
                    }                       
                }
                catch (Exception ex)
                {
                    Debug.Trace(ex);
                    Engine.Instance.Logs.Log(LogType.Fatal, Messages.OptionsReverted);
                    ResetAll(true);
                }
            }
        }
        

	}
}
