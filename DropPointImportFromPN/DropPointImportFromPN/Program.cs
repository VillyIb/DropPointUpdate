using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EU.Iamia.Logging;


using System.Xml;
using System.Net;
using nu.gtx.Communication.Mail;
using System.Net.Mail;
using nu.gtx.Business.PNDropPoints;
using DropPointImportFromPN.Configuations;


namespace DropPointImportFromPN
{
    class Program
    {
       
        private static readonly ILog Logger = LogManager.GetLogger();
        private static Boolean forceReload = false;
       

        static void Main(string[] args)
        {
            if (args.Length>0)
                forceReload = true;

           
            List<ConfigurationGeneral.CarrierAndCountry> carrierAndCountry = ConfigurationGeneral.CarrierAndCountryList;
            foreach (ConfigurationGeneral.CarrierAndCountry cac in carrierAndCountry)
            {
                foreach (string country in cac.Countries)
                {

                    Logger.Debug(String.Format("Execute on country {0} and Carrier {1}", country, cac.CarrierId));
                    Execute(cac.CarrierId, country);
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

    static void Execute(int Carrier, string Country)
        {
            XmlNode errorNode=null;
             
            try
            {
                DropPointService Importer = new DropPointService();
               
                Importer.Country=Country;
                Importer.Carrier=Carrier;

                int no_records = Importer.CountExistingDropPoints(Country, Carrier);
                string urlParameters = string.Format("?apikey={0}&countryCode={1}", ConfigurationGeneral.PN_consumerId, Country);

                Logger.Debug(String.Format("Load document {0}",CreateRequest(urlParameters)));
                XmlDocument xDoc = MakeRequest(CreateRequest(urlParameters));
                Logger.Debug("Document loaded");
                if (xDoc != null)
                {
                    
                    foreach (XmlNode d in xDoc.SelectNodes("//servicePointInformation"))
                    {
                        
                       
                        if (d.SelectSingleNode("eligibleParcelOutlet").InnerText != "false")
                        {
                            errorNode = d;
                            Importer.AddDropPoint(XmlConvertToAddress(d, Carrier));
                            Console.Write("take");
                        }

                    }
                    errorNode = null;
                    if (Math.Abs(no_records - Importer.CountListDropPoints() ) < no_records * 0.1 || no_records == 0||forceReload)
                    {
                        Logger.Debug(String.Format("Update database with {0} records",Importer.CountListDropPoints()));
                        Importer.SaveToDatabase();
                        Logger.Debug("Update done");
                        
                    }
                    else
                    {
                        String error=String.Format("Import of drop points from carrierId = {0} and Country = {1} is not runned due to mismatch between existing data {2} records and imported data {3} records", Carrier, Country, no_records, Importer.CountListDropPoints());
                        Logger.Error(error);
                        SendErrorMail("Import of drop points not executed", error);
                    }

                }
            }
            catch (Exception ex)

            {

                String xml = "";
                if (errorNode != null)
                    xml = errorNode.OuterXml;
                String error=string.Format("Import of drop points from carrierId = {0} and Country = {1} go following error: {2}  xml: {3}", Carrier, Country,ex.Message,xml );
                Logger.Error(error);
                SendErrorMail("Import of drop points stopped due to error", error); 

            }


        }

       



      

        private static string CreateRequest(string queryString)
        {
            string UrlRequest = ConfigurationGeneral.PN_WebserviceURL + queryString;
            return (UrlRequest);
        }

        private static XmlDocument MakeRequest(string requestUrl)
        {   int times=3;

        while (times > 0)
        {
        
            try
            {
                HttpWebRequest request = WebRequest.Create(requestUrl) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(response.GetResponseStream());
                return (xmlDoc);

            }
            catch (Exception e)
            {
                if (--times <= 0)
                    throw;
                else
                    System.Threading.Thread.Sleep(60*1000); 
            }
        }
        return null;
        }





        private static AddressHolder XmlConvertToAddress(XmlNode sp, int carrier)

        {    AddressHolder address =new AddressHolder();
           
            

            address.DropPointCode = loadXmlValue(sp, "servicePointId");
            address.Address1 = loadXmlValue(sp, "deliveryAddress/streetName") + " " + loadXmlValue(sp, "deliveryAddress/streetNumber");
            address.City = loadXmlValue(sp, "deliveryAddress/city");
            address.Country = loadXmlValue(sp, "deliveryAddress/countryCode");
            address.Name = loadXmlValue(sp, "name");
            address.Zip = loadXmlValue(sp, "deliveryAddress/postalCode");
            address.Latitude = loadXmlDouble("coordinates/coordinate[0]/easting", sp);
            address.Longitude = loadXmlDouble("coordinates/coordinate[0]/northing", sp);
            address.SrId = loadXmlValue(sp, "coordinates/coordinate[0]/srId");
            address.Opening = sp.SelectSingleNode("openingHours").InnerXml;
            List<String> ziplist = new List<String>();
            foreach (XmlNode zipNode in sp.SelectNodes("notificationArea/postalCodes/string"))
            {
              ziplist.Add(zipNode.InnerText);
            }
            if (!ziplist.Contains(address.Zip))
            {
                ziplist.Add(address.Zip);
            }
            address.ZipCode = ziplist;
                



            return address;

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


    }


    
}
