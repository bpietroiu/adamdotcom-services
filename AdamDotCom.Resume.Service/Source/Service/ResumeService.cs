﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel;
using AdamDotCom.Common.Service;
using AdamDotCom.Common.Service.Infrastructure;
using AdamDotCom.Common.Service.Utilities;

namespace AdamDotCom.Resume.Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ResumeService : IResume
    {
        public Resume ResumeXml(string firstnameLastname)
        {
            return Resume(firstnameLastname);
        }

        public Resume ResumeJson(string firstnameLastname)
        {
            return Resume(firstnameLastname);
        }

        private static Resume Resume(string firstnameLastname)
        {
            Assert.ValidInput(firstnameLastname, "firstname-lastname");

            firstnameLastname = firstnameLastname.Scrub();

            if (ServiceCache.IsInCache<Resume>(firstnameLastname))
            {
                var cachedResume = (Resume) ServiceCache.GetFromCache<Resume>(firstnameLastname);
                if (cachedResume != null)
                {
                    return cachedResume;
                }
            }

            var linkedInEmailAddress = ConfigurationManager.AppSettings["LinkedInEmailAddress"];
            var linkedInPassword = ConfigurationManager.AppSettings["LinkedInPassword"];

            var resumeSniffer = new LinkedInResumeSniffer(linkedInEmailAddress, linkedInPassword, firstnameLastname);

            Resume resume = null;
            try
            {
                resume = resumeSniffer.GetResume();
            }
            catch (Exception)
            {
                HandleErrors(linkedInEmailAddress);
            }

            HandleErrors(resumeSniffer.Errors);

            return resume.AddToCache(firstnameLastname);
        }

        private static void HandleErrors(List<KeyValuePair<string, string>> errors)
        {
            
            if (errors != null && errors.Count != 0)
            {
                var criticalErrors = errors.Where(e => e.Key.Contains("Critical")).ToList();
                if (criticalErrors.Count != 0)
                {
                    throw new RestException(HttpStatusCode.BadRequest, criticalErrors, (int)ErrorCode.InternalError);
                }
            }
        }

        private static void HandleErrors(string emailAddress)
        {
            throw new RestException(new KeyValuePair<string, string>("LinkedInResumeSniffer",
                                                                     string.Format(
                                                                         "The requested resume could not be retrieved. Ensure that you have added {0} as a LinkedIn contact, alternatively you can download the source code ({1}) and contribute a patch for your resume.",
                                                                         emailAddress,
                                                                         "http://code.google.com/p/adamdotcom-services/source/checkout"))
                );
        }
    }
}