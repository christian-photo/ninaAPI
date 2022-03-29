#region "copyright"

/*
    Copyright © 2022 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ninaAPI
{
    public static class Utility
    {
        public static IList<IDeepSkyObjectContainer> GetAllTargets(this ISequenceMediator sequence)
        {
            IList<IDeepSkyObjectContainer> targets = sequence.GetAllTargetsInAdvancedSequence();
            targets.Concat(sequence.GetAllTargetsInSimpleSequence());
            return targets;
        }

        private static ApplicationStatus Status;
        public static Progress<ApplicationStatus> GetStatus(this IApplicationStatusMediator mediator)
        {
            return new Progress<ApplicationStatus>(p => Status = p);
        }

        public static string MakeString(this IEnumerable<string> list)
        {
            return "[" + string.Join(", ", list) + "]";
        }
        
        public static string MakeString(this Dictionary<string, string>.ValueCollection coll)
        {
            return coll.ToArray().MakeString();
        }

        public static Dictionary<string, string> GetLocalNames()
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            names.Add("LOCALHOST", "localhost");

            string hostName = Dns.GetHostName();
            if (!string.IsNullOrEmpty(hostName))
            {
                names.Add("HOSTNAME", hostName);
            }

            string ipv4 = GetIPv4Address();
            if (!string.IsNullOrEmpty(ipv4))
            {
                names.Add("IPADRESS", ipv4);
            }

            Logger.Debug("Local names: " + names.Values.MakeString());

            return names;
        }

        public static string GetIPv4Address()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }

        public static X509Certificate2 GetCertificateFromStore(string certName)
        {

            // Get the certificate store for the current user.
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Place all certificates in an X509Certificate2Collection object.
                X509Certificate2Collection certCollection = store.Certificates;
                // If using a certificate with a trusted root you do not need to FindByTimeValid, instead:
                // currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, true);
                X509Certificate2Collection currentCerts = certCollection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection signingCert = currentCerts.Find(X509FindType.FindBySubjectDistinguishedName, certName, false);
                if (signingCert.Count == 0)
                    return null;
                // Return the first certificate in the collection, has the right name and is current.
                return signingCert[0];
            }
            finally
            {
                store.Close();
            }
        }
    }
}
