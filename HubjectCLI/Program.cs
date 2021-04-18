/*
 * Copyright (c) 2014-2021 GraphDefined GmbH
 * This file is part of WWCP OICP <https://github.com/OpenChargingCloud/WWCP_OICP>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using cloud.charging.open.protocols.OICPv2_3.CPO;

#endregion

namespace cloud.charging.open.protocols.OICPv2_3.CLI
{

    public enum OutputMode
    {
        text,
        code
    }


    public class Program
    {

        public static void PrintHelp()
        {
            Console.WriteLine("Help...");
            Environment.Exit(0);
        }

        public static void PrintVersion()
        {
            Console.WriteLine("Version...");
            Environment.Exit(0);
        }

        public static void PrintCPOHelp()
        {
            Console.WriteLine("CPO help...");
            Environment.Exit(0);
        }

        public static void PrintEMPHelp()
        {
            Console.WriteLine("EMP help...");
            Environment.Exit(0);
        }

        public static void Fail(String Text, Int32 ExitCode = 1)
        {
            Console.WriteLine(Text);
            Environment.Exit(ExitCode);
        }

        public static void Exit(String Text)
        {
            Console.WriteLine(Text);
            Environment.Exit(0);
        }


        /// <summary>
        /// Hubject Command Line Interface.
        /// </summary>
        /// <param name="Arguments">Command line arguments.</param>
        public static async Task Main(String[] Arguments)
        {

            #region Check 1st argument (command)

            if (Arguments.Length == 0)
                PrintHelp();

            var command = Arguments[0]?.Trim()?.ToLower();

            if (command is null)
                PrintHelp();

            if (command == "help")
            {

                var role = Arguments.Length > 1
                               ? Arguments[1]?.Trim()?.ToLower()
                               : null;

                if (role == null)
                    PrintHelp();

                if (role == "cpo")
                    PrintCPOHelp();

                if (role == "emp")
                    PrintEMPHelp();

            }

            if (command == "version" || command == "-version" || command == "--version")
                PrintVersion();

            #endregion

            #region Parse arguments

            var               output              = OutputMode.text;
            String[]          config              = default;
            X509Certificate2  clientCertificate   = default;
            Operator_Id?      operatorId          = default;
            Provider_Id?      providerId          = default;
            UID?              uid                 = default;
            Identification    identification      = default;
            EVSE_Id?          evseId              = default;

            for (var i=1; i<Arguments.Length; i++)
            {

                #region --config

                if (Arguments[i] == "--config" && Arguments.Length >= i + 1)
                {

                    try
                    {

                        config = File.ReadAllLines(Arguments[i + 1]);

                    }
                    //catch (IOException)
                    catch (Exception e)
                    {
                        Fail("Could not parse the given config file '" + Arguments[i + 1] + "': " + e.Message);
                    }

                    i++;
                    continue;

                }

                #endregion

                #region --output

                if (Arguments[i] == "--output" && Arguments.Length >= i + 1)
                {

                    if (!Enum.TryParse(Arguments[i + 1], true, out output))
                        Fail("Could not parse the given output mode '" + Arguments[i + 1] + "'!");

                    i++;
                    continue;

                }

                #endregion

                #region --clientCertificate

                if (Arguments[i] == "--clientCertificate" && Arguments.Length >= i + 1)
                {

                    try
                    {

                        clientCertificate = new X509Certificate2(Arguments[i + 1], "");

                    }
                    catch (Exception e)
                    {
                        Fail("Could not parse the given client certificate from file '" + Arguments[i + 1] + "': " + e.Message);
                    }

                    i++;
                    continue;

                }

                #endregion


                #region --operatorId

                if (Arguments[i] == "--operatorId" && Arguments.Length >= i + 1)
                {

                    operatorId = Operator_Id.TryParse(Arguments[i + 1]);

                    if (!operatorId.HasValue)
                        Fail("Could not parse the given operator identification '" + Arguments[i + 1] + "'!");

                    i++;
                    continue;

                }

                #endregion

                #region --uid

                if (Arguments[i] == "--uid" && Arguments.Length >= i + 1)
                {

                    uid = UID.TryParse(Arguments[i + 1]);

                    if (!uid.HasValue)
                        Fail("Could not parse the given UID '" + Arguments[i + 1] + "'!");

                    identification = Identification.FromUID(uid.Value);

                    i++;
                    continue;

                }

                #endregion

                #region --evseid

                if (Arguments[i] == "--evseid" && Arguments.Length >= i + 1)
                {

                    evseId = EVSE_Id.TryParse(Arguments[i + 1]);

                    if (!uid.HasValue)
                        Fail("Could not parse the given EVSE identification '" + Arguments[i + 1] + "'!");

                    i++;
                    continue;

                }

                #endregion

            }

            #endregion


            if (command == "AuthorizeStart".ToLower() ||
                command == "AuthorizeStop". ToLower())
            {

                using (var cpoClient = new CPOClient(RemoteCertificateValidator: (sender, certificate, chain, sslPolicyErrors) => true,
                                                     ClientCert:                 clientCertificate)) {

                    #region AuthorizeStart

                    if (command == "AuthorizeStart".ToLower()) {

                        #region Initial checks

                        if (!operatorId.HasValue)
                            Fail(@"The 'AuthorizeStart' command requires an ""--operatorId"" parameter!");

                        if (identification == default)
                            Fail(@"The 'AuthorizeStart' command requires an ""--uid"" parameter!");

                        #endregion

                        var result = await cpoClient.AuthorizeStart(operatorId.Value,
                                                                    identification,
                                                                    evseId);

                        if (result.Result.AuthorizationStatus == AuthorizationStatusTypes.Authorized)
                        {

                            if (output == OutputMode.text)
                                Exit("'" + result.Result.ProviderId + "' authorized '" + (result.Request as AuthorizeStartRequest).Identification + "'" +
                                     (evseId.HasValue ? " at EVSE '" + evseId.Value + "'" : "") +
                                     "!");

                            else if (output == OutputMode.code)
                                Exit("true");

                        }
                        else
                        {

                            if (output == OutputMode.text)
                                Exit("No authorization of '" + (result.Request as AuthorizeStartRequest).Identification + "' " +
                                     (evseId.HasValue ? "at EVSE '" + evseId.Value + "' " : "") +
                                     "!");

                            else if (output == OutputMode.code)
                                Exit("true");

                        }

                    }

                    #endregion

                }

            }

        }

    }
}
