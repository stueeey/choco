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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using adapters;
    using attributes;
    using commandline;
    using configuration;
    using extractors;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using Microsoft.Win32;
    using platforms;
    using resources;
    using services;

    [CommandFor("HandleUrl", "Allows chocolatey to handle choco:// urls")]
    public class ChocolateyHandleUrlCommand : ICommand
    {
        private readonly IChocolateyUrlHandlerService _urlHandlerService;

        public ChocolateyHandleUrlCommand(IChocolateyUrlHandlerService handlerService)
        {
            _urlHandlerService = handlerService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("url=",
                    "Url - Url to handle and turn into a choco command",
                    option => configuration.HandleUrlCommand.Url = option.remove_surrounding_quotes());
            optionSet
                .Add("register",
                    "Registers chocolatey as the handler for the choco:// url protocol",
                    option => configuration.HandleUrlCommand.Instruction = UrlProtocolCommandInstruction.Register);
            optionSet
                .Add("deregister",
                    "De-registers chocolatey as the handler for the choco:// url protocol",
                    option => configuration.HandleUrlCommand.Instruction = UrlProtocolCommandInstruction.Deregister);
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "RegisterUrlProtocol Command");
            this.Log().Info(@"
This will allow chocolatey to handle urls starting with choco://
(e.g. choco://Skype to install skype)

NOTE: This command should only be used when installing Chocolatey or when opening a choco url, not 
 during normal operation. 

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            // noop is passed through in ChocolateyConfiguration
            perform_command(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            perform_command(configuration);
        }

        private void perform_command(ChocolateyConfiguration configuration)
        {
            switch (configuration.HandleUrlCommand.Instruction)
            {
                case UrlProtocolCommandInstruction.ProcessUrl:
                    _urlHandlerService.handle_url(configuration);
                    break;
                case UrlProtocolCommandInstruction.Register:
                    _urlHandlerService.register_url_protocol(configuration);
                    break;
                case UrlProtocolCommandInstruction.Deregister:
                    _urlHandlerService.deregister_url_protocol(configuration);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
    }
}

        public virtual bool may_require_admin_access()
        {
            return true;
        }
    }
}