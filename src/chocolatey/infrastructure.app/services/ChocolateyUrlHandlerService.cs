// Copyright © 2011 - Present RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Text.RegularExpressions;
    using configuration;
    using Microsoft.Win32;
    using platforms;

    internal class ChocolateyUrlHandlerService : IChocolateyUrlHandlerService
    {

        // ReSharper disable InconsistentNaming
        // True constants
        private const string LM_SOFTWARE = @"HKEY_LOCAL_MACHINE\SOFTWARE";
        private const string LM_CLASSES = LM_SOFTWARE + @"\Classes";
        private const string LM_SOFTWARE_REGISTEREDAPPLICATIONS = LM_SOFTWARE + @"\RegisteredApplications";
        private const string URL_PROTOCOL = "URL Protocol";

        // For-all-intents-and-purposes constants
        // This is the command that will be fired when the user opens a choco:// url
        // choco will prompt to run as admin
        // %1 will contain the FULL url
        private static string command => string.Format("\"{0}\" HandleUrl -url=\"%1\"", ApplicationParameters.ChocolateyBootstrapperApplicationPath);
        private static readonly string CHOCOPROTOCOLHANDLER = ApplicationParameters.ChocolateyUrlProtocolPrefix + "ProtocolHandler";
        private static readonly string CR_CHOCO = @"HKEY_CLASSES_ROOT\" + ApplicationParameters.ChocolateyUrlProtocolPrefix;
        private static readonly string CR_CHOCO_SHELL_OPEN_COMMAND = CR_CHOCO + @"\shell\open\command";
        private static readonly string LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER = LM_CLASSES + @"\" + CHOCOPROTOCOLHANDLER;
        private static readonly string LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER_SHELL_OPEN_COMMAND = LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER + @"\shell\open\command";
        private static readonly string LM_SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES_URLASSOCIATIONS = LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER + @"\Capabilities\URLAssociations";
        private static readonly string SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES = @"SOFTWARE\" + CHOCOPROTOCOLHANDLER + @"\Capabilities";
        private static readonly string CHOCOLATEY_COMMAND_NAME = ApplicationParameters.ChocolateyUrlProtocolPrefix + ":Chocolatey Command";
        // ReSharper restore InconsistentNaming

        

        // ================================== Public ==================================

        public void register_url_protocol(ChocolateyConfiguration configuration)
        {
            if (!check_platform_is_windows(configuration))
                return;
            this.Log().Info($"{ApplicationParameters.Name} is registering itself as the handler for urls starting with {ApplicationParameters.ChocolateyUrlProtocolPrefix}://");
            this.Log().Info($"Command is:\n {command}");
            register_basic_handler(configuration.Noop);

            // For Windows 8+, register as a choosable protocol handler.
            // Version detection from http://stackoverflow.com/a/17796139/259953
            this.Log().Info($"OS detected as {configuration.Information.PlatformVersion}");
            var win8Version = new Version(6, 2, 9200, 0);
            if (configuration.Information.PlatformVersion >= win8Version)
                register_chooseable_handler(configuration.Noop);
        }

        public void deregister_url_protocol(ChocolateyConfiguration configuration)
        {
            if (!check_platform_is_windows(configuration))
                return;

            throw new NotImplementedException();
        }

        private static readonly Regex _versionRegex = new Regex(@"(?:-v|-version)=(?<version>[\d\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public void handle_url(ChocolateyConfiguration configuration, IChocolateyPackageService packageService)
        {
            if (string.IsNullOrWhiteSpace(configuration.HandleUrlCommand.Url))
            {
                this.Log().Error("Please specify a url to process");
                return;
            }
            this.Log().Info($"Url is '{configuration.HandleUrlCommand.Url}'");
            this.Log().Info($"Sources: " + configuration.Sources);
            var urlSansPrefix = Regex.Replace(configuration.HandleUrlCommand.Url.Trim(), "^choco://", "", RegexOptions.IgnoreCase);
            var parts = urlSansPrefix.Split(';'); // support for flags
            configuration.PackageNames = parts[0];
            // Only support a safe subset of flags (need to keep them from being able to specify 3rd party package sources)
            for (int i = 1; i < parts.Length; i++)
            {
                if (Regex.IsMatch(parts[i], "-not-?silent|-ns"))
                {
                    configuration.NotSilent = true;
                    continue;
                }
                if (Regex.IsMatch(parts[i], "-y"))
                {
                    configuration.PromptForConfirmation = false;
                    continue;
                }
                var versionMatch = _versionRegex.Match(parts[i]);
                if (versionMatch.Success)
                {
                    var version = versionMatch.Groups["version"].Value;
                    configuration.Version = version;
                    this.Log().Info($"Version requested is {version}");
                    continue;
                }

            }
            if (configuration.Noop)
                packageService.install_noop(configuration);
            else
                packageService.install_run(configuration);

        }

        // ================================== Private ==================================

        private bool check_platform_is_windows(ChocolateyConfiguration configuration)
        {
            // ReSharper disable once InvertIf
            if (configuration.Information.PlatformType != PlatformType.Windows)
            {
                this.Log().Error($"Registering for the url protocol handler is only supported for windows (you are detected as running {configuration.Information.PlatformName})");
                return false;
            }
            return true;
        }

        private void register_chooseable_handler(bool noop)
        {
            this.Log().Info("Registering chooseable url handler (win8 and later)");
            this.Log().Debug($"Creating key [{LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER}] and setting it's default value to {CHOCOLATEY_COMMAND_NAME}");
            if (!noop)
            {
                Registry.SetValue(
                    LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER,
                    string.Empty,
                    CHOCOLATEY_COMMAND_NAME,
                    RegistryValueKind.String);
            }
            this.Log().Debug($"Creating key [{LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER_SHELL_OPEN_COMMAND}] and setting it's default value to {command}");
            if (!noop)
            {
                Registry.SetValue(
                    LM_SOFTWARE_CLASSES_CHOCOPROTOCOLHANDLER_SHELL_OPEN_COMMAND,
                    string.Empty,
                    command,
                    RegistryValueKind.String);
            }
            this.Log().Debug($"Creating key [{LM_SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES_URLASSOCIATIONS}] and setting subkey '{ApplicationParameters.ChocolateyUrlProtocolPrefix}' to {CHOCOPROTOCOLHANDLER}");
            if (!noop)
            {
                Registry.SetValue(
                    LM_SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES_URLASSOCIATIONS,
                    ApplicationParameters.ChocolateyUrlProtocolPrefix,
                    CHOCOPROTOCOLHANDLER,
                    RegistryValueKind.String);
            }
            this.Log().Debug($"Creating key [{LM_SOFTWARE_REGISTEREDAPPLICATIONS}] and setting subkey [{CHOCOPROTOCOLHANDLER}] to {SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES}");
            if (!noop)
            {
                Registry.SetValue(
                    LM_SOFTWARE_REGISTEREDAPPLICATIONS,
                    CHOCOPROTOCOLHANDLER,
                    SOFTWARE_CHOCOPROTOCOLHANDLER_CAPABILITIES,
                    RegistryValueKind.String);
            }
        }

        private void register_basic_handler(bool noop)
        {
            // Register as the default handler for the choco: protocol.
            this.Log().Info($"Registering basic handler for the {ApplicationParameters.ChocolateyUrlProtocolPrefix}:// protocol");
            this.Log().Debug($"Creating key [{CR_CHOCO}] and setting it's default value to {CHOCOLATEY_COMMAND_NAME}");
            if (!noop)
            {
                Registry.SetValue(
                    CR_CHOCO,
                    string.Empty,
                    CHOCOLATEY_COMMAND_NAME,
                    RegistryValueKind.String);
            }
            this.Log().Debug($"Creating subkey [{URL_PROTOCOL}] in key [{CR_CHOCO}]");
            if (!noop)
            {
                Registry.SetValue(
                    CR_CHOCO,
                    URL_PROTOCOL,
                    string.Empty,
                    RegistryValueKind.String);
            }
            this.Log().Debug($"Creating key [{CR_CHOCO}] and setting it's default value to {command}");
            if (!noop)
            {
                Registry.SetValue(
                    CR_CHOCO_SHELL_OPEN_COMMAND,
                    string.Empty,
                    command,
                    RegistryValueKind.String);
            }
        }
    }
}
