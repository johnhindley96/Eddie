﻿// <eddie_source_header>
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
using System.IO;
using System.Text;
using System.Xml;
using Eddie.Core;
using Eddie.Common;

namespace Eddie.Platform.Linux
{
	public class NetworkLockIptables : NetworkLockPlugin
	{
		private IpAddresses m_ipsWhiteListOutgoing = new IpAddresses();
		private bool m_supportIPv4 = true;
		private bool m_supportIPv6 = true;
		private string m_iptablesVersion = "";

		public override string GetCode()
		{
			return "linux_iptables";
		}

		public override string GetName()
		{
			return "Linux iptables";
		}

		public override bool GetSupport()
		{
			if (Platform.Instance.LocateExecutable("iptables") == "")
				return false;
			if (Platform.Instance.LocateExecutable("iptables-save") == "")
				return false;
			if (Platform.Instance.LocateExecutable("iptables-restore") == "")
				return false;
			if (Platform.Instance.LocateExecutable("ip6tables") == "")
				return false;
			if (Platform.Instance.LocateExecutable("ip6tables-save") == "")
				return false;
			if (Platform.Instance.LocateExecutable("ip6tables-restore") == "")
				return false;

			return true;
		}

		public string GetBackupPath(string ipVersion)
		{
			if (ipVersion == "4") // For compatibility with Eddie<2.9
				ipVersion = "";
			return Engine.Instance.Storage.GetPathInData("ip" + ipVersion + "tables.dat");
		}

		public string DoIptablesShell(string exe, string args)
		{
			return DoIptablesShell(exe, args, true);
		}

		public string DoIptablesShell(string exe, string args, bool fatal)
		{
			lock (this)
			{
				// 2.14.0
				/*
				SystemShell s = new SystemShell();
				s.Path = Platform.Instance.LocateExecutable(exe);				
				if (UtilsCore.CompareVersions(m_iptablesVersion, "1.4.21") >= 0)
				{
					// 2.13.6 - The version 1.4.21 is generic Debian8. I don't find in official
					// changelogs https://www.netfilter.org/projects/iptables/downloads.html
					// the correct version. For sure don't exists in 1.4.14 of Debian7.
					args = "--wait " + args;
				}				
				if (args != "")
					s.Arguments.Add(args); // Exception: all arguments as one, it works.
				if (fatal)
					s.ExceptionIfFail = true;
				s.Run();				
				return s.StdOut;
				*/

				// 2.14.1: Previous version use --wait if iptables >1.4.21. But there are issues about distro, even in the latest Debian unstable (2018).
				int nTry = 10;
				string lastestOutput = "";
				for (int iTry = 0; iTry < 10; iTry++)
				{
					SystemShell s = new SystemShell();
					s.Path = Platform.Instance.LocateExecutable(exe);
					if (args != "")
						s.Arguments.Add(args); // Exception: all arguments as one, it works.
					if ((fatal) && (iTry == nTry - 1))
						s.ExceptionIfFail = true;
					s.Run();
					if (s.StdErr.ToLowerInvariant().Contains("temporarily unavailable")) // Older Debian (iptables without --wait)
					{
						System.Threading.Thread.Sleep(500);
						continue;
					}
					if (s.StdErr.ToLowerInvariant().Contains("xtables lock")) // Newest Debian (iptables with --wait but not automatic)
					{
						System.Threading.Thread.Sleep(500);
						continue;
					}
					lastestOutput = s.StdOut;
					return lastestOutput;
				}
				return lastestOutput;
			}
		}

		public override void Init()
		{
			base.Init();

			m_iptablesVersion = SystemShell.Shell1(Platform.Instance.LocateExecutable("iptables"), "--version");
			m_iptablesVersion = m_iptablesVersion.Replace("iptables v", "");
		}

		public override void Activation()
		{
			base.Activation();

			string rulesBackupSessionV4 = GetBackupPath("4");
			string rulesBackupSessionV6 = GetBackupPath("6");

			try
			{
				if ((Platform.Instance.FileExists(rulesBackupSessionV4)) || (Platform.Instance.FileExists(rulesBackupSessionV6)))
					throw new Exception(Messages.NetworkLockLinuxUnexpectedAlreadyActive);

				// IPv4 assumed, if not available, will throw a fatal exception.

				// IPv6 Test
				{
					SystemShell s = new SystemShell();
					s.Path = Platform.Instance.LocateExecutable("ip6tables");
					s.Arguments.Add("-L");
					m_supportIPv6 = s.Run();

					if (m_supportIPv6 == false)
						Engine.Instance.Logs.Log(LogType.Verbose, Messages.NetworkLockLinuxIPv6NotAvailable);
				}


				if (m_supportIPv4)
				{
					// IPv4 - Backup
					Platform.Instance.FileContentsWriteText(rulesBackupSessionV4, DoIptablesShell("iptables-save", ""), Encoding.ASCII);
				}

				if (m_supportIPv6)
				{
					// IPv6 - Backup
					Platform.Instance.FileContentsWriteText(rulesBackupSessionV6, DoIptablesShell("ip6tables-save", ""), Encoding.ASCII);
				}

				if (m_supportIPv4)
				{
					// IPv4 - Flush
					DoIptablesShell("iptables", "-P INPUT ACCEPT");
					DoIptablesShell("iptables", "-P FORWARD ACCEPT");
					DoIptablesShell("iptables", "-P OUTPUT ACCEPT");
					DoIptablesShell("iptables", "-t nat -F", false);
					DoIptablesShell("iptables", "-t mangle -F", false);
					DoIptablesShell("iptables", "-F");
					DoIptablesShell("iptables", "-X");
				}

				if (m_supportIPv6)
				{
					// IPv6 - Flush
					DoIptablesShell("ip6tables", "-P INPUT ACCEPT");
					DoIptablesShell("ip6tables", "-P FORWARD ACCEPT");
					DoIptablesShell("ip6tables", "-P OUTPUT ACCEPT");
					DoIptablesShell("ip6tables", "-t nat -F", false);
					DoIptablesShell("ip6tables", "-t mangle -F", false);
					DoIptablesShell("ip6tables", "-F");
					DoIptablesShell("ip6tables", "-X");
				}

				if (m_supportIPv4)
				{
					// IPv4 - Local
					DoIptablesShell("iptables", "-A INPUT -i lo -j ACCEPT");
					DoIptablesShell("iptables", "-A OUTPUT -o lo -j ACCEPT");
				}

				if (m_supportIPv6)
				{
					// IPv6 - Local
					DoIptablesShell("ip6tables", "-A INPUT -i lo -j ACCEPT");
					// Reject traffic to localhost that does not originate from lo0.
					DoIptablesShell("ip6tables", "-A INPUT ! -i lo -s ::1/128 -j REJECT"); // 2.14.0
					DoIptablesShell("ip6tables", "-A OUTPUT -o lo -j ACCEPT");
				}

				if (m_supportIPv6)
				{
					// IPv6 - Disable processing of any RH0 packet which could allow a ping-pong of packets
					DoIptablesShell("ip6tables", "-A INPUT -m rt --rt-type 0 -j DROP");
					DoIptablesShell("ip6tables", "-A OUTPUT -m rt --rt-type 0 -j DROP");
					DoIptablesShell("ip6tables", "-A FORWARD -m rt --rt-type 0 -j DROP");
				}

				if (m_supportIPv6) // 2.14.0
				{
					// IPv6 - Rules which are required for your IPv6 address to be properly allocated
					DoIptablesShell("ip6tables", "-A INPUT -p icmpv6 --icmpv6-type router-advertisement -m hl --hl-eq 255 -j ACCEPT");
					DoIptablesShell("ip6tables", "-A INPUT -p icmpv6 --icmpv6-type neighbor-solicitation -m hl --hl-eq 255 -j ACCEPT");
					DoIptablesShell("ip6tables", "-A INPUT -p icmpv6 --icmpv6-type neighbor-advertisement -m hl --hl-eq 255 -j ACCEPT");
					DoIptablesShell("ip6tables", "-A INPUT -p icmpv6 --icmpv6-type redirect -m hl --hl-eq 255 -j ACCEPT");
				}

				if (m_supportIPv4)
				{
					// IPv4 - Make sure you can communicate with any DHCP server
					DoIptablesShell("iptables", "-A OUTPUT -d 255.255.255.255 -j ACCEPT");
					DoIptablesShell("iptables", "-A INPUT -s 255.255.255.255 -j ACCEPT");
				}

				if (Engine.Instance.Storage.GetBool("netlock.allow_private"))
				{
					if (m_supportIPv4)
					{
						// IPv4 - Private networks
						DoIptablesShell("iptables", "-A INPUT -s 192.168.0.0/16 -d 192.168.0.0/16 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 192.168.0.0/16 -j ACCEPT");
						DoIptablesShell("iptables", "-A INPUT -s 10.0.0.0/8 -d 10.0.0.0/8 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 10.0.0.0/8 -d 10.0.0.0/8 -j ACCEPT");
						DoIptablesShell("iptables", "-A INPUT -s 172.16.0.0/12 -d 172.16.0.0/12 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 172.16.0.0/12 -d 172.16.0.0/12 -j ACCEPT");

						// IPv4 - Multicast
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 224.0.0.0/24 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 224.0.0.0/24 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 224.0.0.0/24 -j ACCEPT");

						// IPv4 - 239.255.255.250  Simple Service Discovery Protocol address
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.250/32 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.250/32 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.250/32 -j ACCEPT");

						// IPv4 - 239.255.255.253  Service Location Protocol version 2 address
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.253/32 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.253/32 -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -s 192.168.0.0/16 -d 239.255.255.253/32 -j ACCEPT");
					}

					if (m_supportIPv6)
					{
						// IPv6 - Allow Link-Local addresses
						DoIptablesShell("ip6tables", "-A INPUT -s fe80::/10 -j ACCEPT");
						DoIptablesShell("ip6tables", "-A OUTPUT -s fe80::/10 -j ACCEPT");

						// IPv6 - Allow multicast
						DoIptablesShell("ip6tables", "-A INPUT -d ff00::/8 -j ACCEPT");
						DoIptablesShell("ip6tables", "-A OUTPUT -d ff00::/8 -j ACCEPT");
					}
				}

				if (Engine.Instance.Storage.GetBool("netlock.allow_ping"))
				{
					if (m_supportIPv4)
					{
						// IPv4
						DoIptablesShell("iptables", "-A INPUT -p icmp --icmp-type echo-request -j ACCEPT");
						DoIptablesShell("iptables", "-A OUTPUT -p icmp --icmp-type echo-reply -j ACCEPT"); // 2.14.0
					}

					if (m_supportIPv6)
					{
						// IPv6
						DoIptablesShell("ip6tables", "-A INPUT -p icmpv6 -j ACCEPT");
						DoIptablesShell("ip6tables", "-A OUTPUT -p icmpv6 -j ACCEPT");
					}
				}

				if (m_supportIPv4)
				{
					// IPv4 - Allow established sessions to receive traffic
					DoIptablesShell("iptables", "-A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT");
				}

				if (m_supportIPv6)
				{
					// IPv6 - Allow established sessions to receive traffic
					DoIptablesShell("ip6tables", "-A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT");
				}

				if (m_supportIPv4)
				{
					// IPv4 - Allow TUN
					DoIptablesShell("iptables", "-A INPUT -i tun+ -j ACCEPT");
					DoIptablesShell("iptables", "-A FORWARD -i tun+ -j ACCEPT");
					DoIptablesShell("iptables", "-A OUTPUT -o tun+ -j ACCEPT");
				}

				if (m_supportIPv6)
				{
					// IPv6 - Allow TUN 
					DoIptablesShell("ip6tables", "-A INPUT -i tun+ -j ACCEPT");
					DoIptablesShell("ip6tables", "-A FORWARD -i tun+ -j ACCEPT");
					DoIptablesShell("ip6tables", "-A OUTPUT -o tun+ -j ACCEPT");
				}

				if (m_supportIPv4)
				{
					// IPv4 - General rule
					DoIptablesShell("iptables", "-A FORWARD -j DROP");
					if (Engine.Instance.Storage.Get("netlock.incoming") == "allow")
						DoIptablesShell("iptables", "-A INPUT -j ACCEPT");
					else
						DoIptablesShell("iptables", "-A INPUT -j DROP");
					if (Engine.Instance.Storage.Get("netlock.outgoing") == "allow")
						DoIptablesShell("iptables", "-A OUTPUT -j ACCEPT");
					else
						DoIptablesShell("iptables", "-A OUTPUT -j DROP");
				}

				if (m_supportIPv6)
				{
					// IPv6 - General rule
					DoIptablesShell("ip6tables", "-A FORWARD -j DROP");
					if (Engine.Instance.Storage.Get("netlock.incoming") == "allow")
						DoIptablesShell("ip6tables", "-A INPUT -j ACCEPT");
					else
						DoIptablesShell("ip6tables", "-A INPUT -j DROP");
					if (Engine.Instance.Storage.Get("netlock.outgoing") == "allow")
						DoIptablesShell("ip6tables", "-A OUTPUT -j ACCEPT");
					else
						DoIptablesShell("ip6tables", "-A OUTPUT -j DROP");
				}

				OnUpdateIps();
			}
			catch (Exception ex)
			{
				Deactivation();
				throw new Exception(ex.Message);
			}
		}

		public override void Deactivation()
		{
			base.Deactivation();

			// IPv4
			string rulesBackupSessionV4 = GetBackupPath("4");

			if (Platform.Instance.FileExists(rulesBackupSessionV4))
			{
				// Flush
				DoIptablesShell("iptables", "-P INPUT ACCEPT");
				DoIptablesShell("iptables", "-P FORWARD ACCEPT");
				DoIptablesShell("iptables", "-P OUTPUT ACCEPT");
				DoIptablesShell("iptables", "-t nat -F", false);
				DoIptablesShell("iptables", "-t mangle -F", false);
				DoIptablesShell("iptables", "-F");
				DoIptablesShell("iptables", "-X");

				// Restore backup - Exception: ShellCmd because ip6tables-restore accept only stdin
				SystemShell.ShellCmd(Platform.Instance.LocateExecutable("iptables-restore") + " <\"" + SystemShell.EscapePath(rulesBackupSessionV4) + "\"");

				Platform.Instance.FileDelete(rulesBackupSessionV4);
			}

			// IPv6
			string rulesBackupSessionV6 = GetBackupPath("6");

			if (Platform.Instance.FileExists(rulesBackupSessionV6))
			{
				// Restore
				DoIptablesShell("ip6tables", "-P INPUT ACCEPT");
				DoIptablesShell("ip6tables", "-P FORWARD ACCEPT");
				DoIptablesShell("ip6tables", "-P OUTPUT ACCEPT");
				DoIptablesShell("ip6tables", "-t nat -F", false);
				DoIptablesShell("ip6tables", "-t mangle -F", false);
				DoIptablesShell("ip6tables", "-F");
				DoIptablesShell("ip6tables", "-X");

				// Restore backup - Exception: ShellCmd because ip6tables-restore accept only stdin
				SystemShell.ShellCmd(Platform.Instance.LocateExecutable("ip6tables-restore") + " <\"" + SystemShell.EscapePath(rulesBackupSessionV6) + "\"");

				Platform.Instance.FileDelete(rulesBackupSessionV6);
			}

			// IPS
			m_ipsWhiteListOutgoing.Clear();
		}

		public override void OnUpdateIps()
		{
			base.OnUpdateIps();

			IpAddresses ipsWhiteListOutgoing = GetIpsWhiteListOutgoing(true);

			// Remove IP not present in the new list
			foreach (IpAddress ip in m_ipsWhiteListOutgoing.IPs)
			{
				if (ipsWhiteListOutgoing.Contains(ip) == false)
				{
					// Remove
					if (ip.IsV4)
					{
						if (m_supportIPv4)
							DoIptablesShell("iptables", "-D OUTPUT -d " + ip.ToCIDR() + " -j ACCEPT");
					}
					else if (ip.IsV6)
					{
						if (m_supportIPv6)
							DoIptablesShell("ip6tables", "-D OUTPUT -d " + ip.ToCIDR() + " -j ACCEPT");
					}
				}
			}

			// Add IP
			foreach (IpAddress ip in ipsWhiteListOutgoing.IPs)
			{
				if (m_ipsWhiteListOutgoing.Contains(ip) == false)
				{
					// Add
					if (ip.IsV4)
					{
						if (m_supportIPv4)
							DoIptablesShell("iptables", "-I OUTPUT 1 -d " + ip.ToCIDR() + " -j ACCEPT");
					}
					else if (ip.IsV6)
					{
						if (m_supportIPv6)
							DoIptablesShell("ip6tables", "-I OUTPUT 1 -d " + ip.ToCIDR() + " -j ACCEPT");
					}
				}
			}

			m_ipsWhiteListOutgoing = ipsWhiteListOutgoing;
		}
	}
}
