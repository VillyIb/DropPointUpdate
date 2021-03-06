﻿using System;
using System.Collections.Generic;
using System.Configuration;


// ReSharper disable SimplifyConditionalTernaryExpression
// ReSharper disable InconsistentNaming

namespace DropPointImportFromGLS.Configuations
{
    /// <summary>
    /// Summary description for ConfigurationGeneral
    /// </summary>
    public static class ConfigurationGeneral
    {
        ///// <summary>
        ///// Returns the Web-system (Home) IATA Country Code. 
        ///// </summary>
        //public static String HomeIataCountryCode
        //{
        //    get
        //    {
        //        var result = ConfigurationManager.AppSettings["HomeIataCountryCode"];
        //        return result.ValueOrDefault("DK");
        //    }
        //}

        ///// <summary>
        ///// Returns the Web-system (Home) Currency Code. 
        ///// </summary>
        //public static String HomeCurrencyCode
        //{
        //    get
        //    {
        //        var result = ConfigurationManager.AppSettings["HomeCurrencyCode"];
        //        return result.ValueOrDefault("DKK");
        //    }
        //}



        /// <summary>
        /// Returns the RootDir for the curent Website
        /// </summary>


        public class CarrierAndCountry
        {
            public int CarrierId { get; set; }
            public string[] Countries { get; set; }

        }


        public static String ErrorMailReciever
        {
            get
            {

                return ConfigurationManager.AppSettings["ErrorMailReciever"];
            }
        }

        public static String ErrorMailSender
        {
            get
            {
                return ConfigurationManager.AppSettings["ErrorMailSender"];

            }
        }



        public static String GLSParcelShopUrl
        {
            get
            {

                return ConfigurationManager.AppSettings["GLSParcelShopUrl"];
            }
        }

        public static List<CarrierAndCountry> CarrierAndCountryList
        {
            get
            {
                List<CarrierAndCountry> carrierCodes = new List<CarrierAndCountry>();
                String carriers = ConfigurationManager.AppSettings["CarrierCodesToRun"];
                foreach (String cp in carriers.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    CarrierAndCountry cac = new CarrierAndCountry();
                    cac.CarrierId = Convert.ToInt16(cp);
                    carrierCodes.Add(cac);
                    String countries = ConfigurationManager.AppSettings["Countries"];
                    cac.Countries = countries.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }



                return carrierCodes;

            }
        }



    }






}