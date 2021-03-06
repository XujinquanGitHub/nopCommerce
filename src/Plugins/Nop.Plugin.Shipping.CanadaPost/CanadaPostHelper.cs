using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Nop.Plugin.Shipping.CanadaPost
{
    /// <summary>
    /// Represents Canada Post helper
    /// </summary>
    public class CanadaPostHelper
    {
        #region Methods

        /// <summary>
        /// Get shipping services for a shipment
        /// </summary>
        /// <param name="mailingScenario">Input parameters</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="isSandbox">Is sandbox (testing environment) used</param>
        /// <param name="errors">Errors</param>
        /// <returns>Shipping services</returns>
        public static pricequotes GetShippingRates(mailingscenario mailingScenario, string apiKey, bool isSandbox, out string errors)
        {
            var parameters = new StringBuilder();
            var xmlWriter = XmlWriter.Create(parameters);
            xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
            var serializerRequest = new XmlSerializer(typeof(mailingscenario));
            serializerRequest.Serialize(xmlWriter, mailingScenario);

            var method = WebRequestMethods.Http.Post;
            var acceptType = "application/vnd.cpc.ship.rate-v3+xml";
            var url = $"{GetBaseUrl(isSandbox)}/rs/ship/price";
            var response = Request(parameters.ToString(), apiKey, method, acceptType, url, out errors);
            if (response == null)
                return null;

            try
            {
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var serializerResponse = new XmlSerializer(typeof(pricequotes));
                    return (pricequotes)serializerResponse.Deserialize(streamReader);
                }
            }
            catch (Exception e)
            {
                errors = e.Message;
                return null;
            }
        }

        /// <summary>
        /// Get list of available services
        /// </summary>
        /// <param name="countryCode">Two-letter ISO code of destination country</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="isSandbox">Is sandbox (testing environment) used</param>
        /// <param name="errors">Errors</param>
        /// <returns>List of services</returns>
        public static services GetServices(string countryCode, string apiKey, bool isSandbox, out string errors)
        {
            var method = WebRequestMethods.Http.Get;
            var acceptType = "application/vnd.cpc.ship.rate-v3+xml";
            var url = $"{GetBaseUrl(isSandbox)}/rs/ship/service";

            if (!string.IsNullOrEmpty(countryCode))
                url += $"?country={countryCode}";

            var response = Request(null, apiKey, method, acceptType, url, out errors);
            if (response == null)
                return null;

            try
            {
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var serializerResponse = new XmlSerializer(typeof(services));
                    return (services)serializerResponse.Deserialize(streamReader);
                }
            }
            catch (Exception e)
            {
                errors = e.Message;
                return null;
            }
        }

        /// <summary>
        /// Get service details
        /// </summary>
        /// <param name="apiKey">The API key</param>
        /// <param name="url">URL endpoint</param>
        /// <param name="acceptType">Request accept type</param>
        /// <param name="errors">Errors</param>
        /// <returns>Service object</returns>
        public static service GetServiceDetails(string apiKey, string url, string acceptType, out string errors)
        {
            var method = WebRequestMethods.Http.Get;
            var response = Request(null, apiKey, method, acceptType, url, out errors);
            if (response == null)
                return null;

            try
            {
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var serializerResponse = new XmlSerializer(typeof(service));
                    return (service)serializerResponse.Deserialize(streamReader);
                }
            }
            catch (Exception e)
            {
                errors = e.Message;
                return null;
            }
        }

        /// <summary>
        /// Get tracking details
        /// </summary>
        /// <param name="trackingNumber">Tracking number</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="isSandbox">Is sandbox (testing environment) used</param>
        /// <param name="errors">Errors</param>
        /// <returns>Tracking details</returns>
        public static trackingdetail GetTrackingDetails(string trackingNumber, string apiKey, bool isSandbox, out string errors)
        {
            var method = WebRequestMethods.Http.Get;
            var acceptType = "application/vnd.cpc.track+xml";
            var url = $"{GetBaseUrl(isSandbox)}/vis/track/pin/{trackingNumber}/detail";

            var response = Request(null, apiKey, method, acceptType, url, out errors);

            if (response == null)
                return null;

            try
            {
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var serializerResponse = new XmlSerializer(typeof(trackingdetail));
                    return (trackingdetail)serializerResponse.Deserialize(streamReader);
                }
            }
            catch (Exception e)
            {
                errors = e.Message;
                return null;
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get base URL of Canada Post API
        /// </summary>
        /// <param name="isSandbox">Is sandbox (testing environment) used</param>
        /// <returns>URL</returns>
        private static string GetBaseUrl(bool isSandbox)
        {
            return isSandbox ? "https://ct.soa-gw.canadapost.ca" : "https://soa-gw.canadapost.ca";
        }

        /// <summary>
        /// Make request to the Canada Post endpoint
        /// </summary>
        /// <param name="parameters">Request body</param>
        /// <param name="apiKey">The API key</param>
        /// <param name="method">Request method</param>
        /// <param name="acceptType">Request accept type</param>
        /// <param name="url">URL endpoint</param>
        /// <param name="errors">Errors</param>
        /// <returns>Response from Canada Post API or null if an error occurred</returns>
        private static HttpWebResponse Request(string parameters, string apiKey, string method, string acceptType, string url, out string errors)
        {
            var errorBuilder = new StringBuilder();
            try
            {
                var authorization = Convert.ToBase64String(Encoding.Default.GetBytes(apiKey));

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Accept = acceptType;
                request.Headers.Add(HttpRequestHeader.Authorization, $"Basic {authorization}");
                request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-CA");

                if (method == WebRequestMethods.Http.Post)
                {
                    var postData = Encoding.Default.GetBytes(parameters);
                    request.ContentLength = postData.Length;
                    request.ContentType = acceptType;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(postData, 0, postData.Length);
                    }
                }

                var response = (HttpWebResponse)request.GetResponse();
                errors = errorBuilder.ToString();

                return response;
            }
            catch (WebException ex)
            {
                try
                {
                    var response = (HttpWebResponse)ex.Response;
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var serializerResponse = new XmlSerializer(typeof(messages));
                        var errorMessages = (messages)serializerResponse.Deserialize(streamReader);
                        if (errorMessages != null)
                        {
                            foreach (var error in errorMessages.message)
                            {
                                errorBuilder.AppendLine($"Canada Post error {error.code}: {error.description}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    errorBuilder.AppendLine(e.Message);
                }
            }
            catch (Exception e)
            {
                errorBuilder.AppendLine(e.Message);
            }

            errors = errorBuilder.ToString();
            return null;
        }

        #endregion
    }
}