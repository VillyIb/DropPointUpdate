using System;
using System.Collections.Generic;
using EU.Iamia.Logging;
using System.Xml;
using System.Net;
using nu.gtx.Communication.Mail;
using System.Net.Mail;
using DropPointImportFromGLS.Configuations;
using nu.gtx.Business.PNDropPoints;




namespace DropPointImportFromGLS
{
    class Program
    {

        private static readonly ILog Logger = LogManager.GetLogger();
        private static Boolean forceReload = false;


        static void Main(string[] args)
        {
            if (args.Length > 0)
                forceReload = true;


            List<ConfigurationGeneral.CarrierAndCountry> carrierAndCountry = ConfigurationGeneral.CarrierAndCountryList;
            foreach (ConfigurationGeneral.CarrierAndCountry cac in carrierAndCountry)
            {
                foreach (string country in cac.Countries)
                {

                    Logger.Debug(String.Format("Execute on country {0} and Carrier {1}", country, cac.CarrierId));
                    Execute(country, cac.CarrierId);
                    Logger.Debug("Execute done");
                }


            }

        }

        static private void SendErrorMail(string subject, string body)
        {
            string toMail = ConfigurationGeneral.ErrorMailReciever;
            MailAddress sender = new MailAddress(ConfigurationGeneral.ErrorMailSender);
            using (var mailcontext = new MailContext { SettingsSuppressExceptions = true })
            {
                //mailcontext.BccList = BccList;
                //mailcontext.CcList ;
                mailcontext.From = sender;
                //mailcontext.ReplyToList = 
                mailcontext.Sender = sender;
                mailcontext.ToList.Add(new MailAddress(toMail));

                mailcontext.Send(subject, body);
            }

        }

        public static double loadXmlDouble(String inputNodeName, XmlNode node)
        {
            double value = 0;
            var xmlElt = node[inputNodeName];
            if (xmlElt != null)
            {
                value = Convert.ToDouble(xmlElt.InnerText);
            }

            return value;
        }


        private static string loadXmlValue(XmlNode n, string path)
        {
            XmlNode node = n.SelectSingleNode(path);
            if (node != null)
                return node.InnerText;
            return "";
        }




        private static AddressHolder XmlConvertToAddress(XmlNode sp, int carrier)
        {
            AddressHolder address = new AddressHolder();



            address.DropPointCode = loadXmlValue(sp, "Number");
            address.Address1 = loadXmlValue(sp, "Streetname");
            address.City = loadXmlValue(sp, "CityName");
            address.Country = loadXmlValue(sp, "CountryCodeISO3166A2");
            address.Name = loadXmlValue(sp, "CompanyName");
            address.Zip = loadXmlValue(sp, "ZipCode");
            address.Latitude = loadXmlDouble("Latitude", sp);
            address.Longitude = loadXmlDouble("Longitude", sp);
            address.SrId = loadXmlValue(sp, "coordinates/coordinate[0]/srId");
            address.Opening = sp.SelectSingleNode("OpeningHours").InnerXml;
            List<String> ziplist = new List<String>();
            address.ZipCode = ziplist;
            




            return address;

        }


        static  Dictionary<string, AddressHolder> LoadDropPoints(int Carrier, string Country)
        {  int countErrors=0;
            XmlNode errorNode = null;

            Logger.Debug(String.Format("Start to loop the zip codes from {0}", Country)); 
            Dictionary<string, AddressHolder> dirAddresses = new Dictionary<string, AddressHolder>();
            DropPointService Importer = new DropPointService();
            foreach (String zip in Importer.GetDistinctZipFromCountry(Country, 15))
            {
                try
                {
                   XmlNode xDoc = MakeRequest(CreateRequest(zip));
                    
                    if (xDoc != null)
                    {
                        Console.Write("run " + zip);
                        Console.Write("no found " + xDoc.SelectNodes("//PakkeshopData").Count);
                        foreach (XmlNode d in xDoc.SelectNodes("//PakkeshopData"))
                        {


                            
                                AddressHolder ah = XmlConvertToAddress(d, Carrier);
                                if (!dirAddresses.ContainsKey(ah.DropPointCode))
                                {
                                    ah.ZipCode.Add(zip);


                                    dirAddresses.Add(ah.DropPointCode, ah);


                                }
                                else
                                {
                                    ah = dirAddresses[ah.DropPointCode];
                                    ah.ZipCode.Add(zip);
                                }


                            }

                        }

                   
                }

                catch (Exception ex)
                {

                    String xml = "";
                    if (errorNode != null)
                        xml = errorNode.OuterXml;
                    String error = string.Format("Import of drop points from carrierId = {0} and Country = {1} go following error: {2}  xml: {3}", Carrier, Country, ex.Message, xml);
                    Logger.Error(error);
                    countErrors++;


                }

               

            }
            Logger.Debug(String.Format("Number of DropPoint: {0} in Country: {1} and Errors :{2}", dirAddresses.Count, Country, countErrors));
            return dirAddresses;


        }

        private static void Execute( String Country, int Carrier)
        {
            
            
            Dictionary<string, AddressHolder> dirAddress =LoadDropPoints(Carrier,Country);
            if(dirAddress !=null && dirAddress.Count>0){
            DropPointService Importer = new DropPointService();
            int no_records = Importer.CountExistingDropPoints(Country, Carrier);
           
                
            
            if (Math.Abs(no_records - dirAddress.Count) < no_records * 0.1 || no_records == 0 || forceReload)
            {
                foreach (AddressHolder address in dirAddress.Values)
                {

                    Importer.AddDropPoint(address);
                    Console.Write("take zip : " + address.Zip);


                }
                Logger.Debug(String.Format("Update database with {0} records", Importer.CountListDropPoints()));
                Importer.SaveToDatabase();
                Logger.Debug("Update done");

            }
            else
            {
                String error = String.Format("Import of drop points from carrierId = {0} and Country = {1} is not runned due to mismatch between existing data {2} records and imported data {3} records", Carrier, Country, no_records, dirAddress.Count);
                Logger.Error(error);
                SendErrorMail("Import of drop points not executed", error);
            }
            }

        }






        private static string CreateRequest(string zip)
        {
            string UrlRequest = ConfigurationGeneral.GLSParcelShopUrl + zip;
            return (UrlRequest);
        }

        private static XmlNode MakeRequest(string requestUrl)
        {
            int times = 3;

            while (times > 0)
            {

                try
                {
                    HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(response.GetResponseStream());

                    return new XmlStripper().RemoveAllNamespaces(xmlDoc.ChildNodes[1]); ;

                }
                catch (Exception e)
                {
                    if (--times <= 0)
                        throw;
                    else
                        System.Threading.Thread.Sleep(60 * 1000);
                }
            }
            return null;
        }






        public static int loadXmlInt(String inputNodeName, XmlNode node)
        {
            var value = new int();
            var xmlElt = node[inputNodeName];
            if (xmlElt != null)
            {

                try
                {
                    value = int.Parse(xmlElt.InnerText);
                }
                catch (Exception)
                {
                }
            }

            return value;
        }

        


    }
}
