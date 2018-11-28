using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PriceTracker
{
    public class APIClient
	{
		// Immutable default definitions
		private const string DEFAULT_URL = "https://poloniex.com/public?command=returnTicker";
		private const string DEFAULT_PRICEKEY = "last";
		private const string DEFAULT_CURRPAIR = "BTC_ETH";
		private double DEFAULT_PRICEINTERVAL = 10;// 10 seconds

		// Mutable definitons as per user's response
		private string _url;
		private string _priceKey;
		private string _currPair;
		private string _jsonPath;
		private TimeSpan _priceInterval = TimeSpan.FromSeconds(60);

		// Return evaluated json query path as per mutable definitions updated by user or through defaults
		// for more details on creating json query path, please find the link: https://www.newtonsoft.com/json/help/html/SelectToken.htm 
		private string JsonPath => string.Format("$.{0}.{1}", _currPair, _priceKey);

		// Constructor initialization with finalizing variables
		public APIClient(string url = DEFAULT_URL, string priceKey = DEFAULT_PRICEKEY, string currPair = DEFAULT_CURRPAIR, double? priceInterval = null)
		{
			this._url = url;
			this._priceInterval = TimeSpan.FromSeconds(priceInterval ?? DEFAULT_PRICEINTERVAL);
      this._priceKey = priceKey;
			this._currPair = currPair;
			this._jsonPath = JsonPath;
		}

		//Get sampling length for calculating simple moving average
		Func<Dictionary<int,decimal>, int> SampleLength = (lstItems) =>
		{
			if(lstItems.Count >= 2)
			{
				return (lstItems.Count % 2 == 0 ? lstItems.Count / 2 : lstItems.Count-1 / 2);
			}
			else
			{
				return 0;
			}
		};
		
		//Calculate simple moving average
		public decimal GetAverage(Dictionary<int, decimal> lstPrcItems) 
		{
			List<decimal> averages = new List<decimal>();
			//Measuring time to perform average calculation -- currently is commented out as it was only for informational purpose during development
			//FinanacialFunctionsExtensions<decimal>.MeasureTime(" Time to calculate Average"
			//,() => 
			//	{
					averages = FinanacialFunctionsExtensions<decimal>.SimpleMovingAverage(lstPrcItems.Select(t => t.Value), SampleLength(lstPrcItems)).ToList();
			//	}
			//);
			var results = lstPrcItems.Zip(averages, (v, a) => new { index = v.Key, Value = v, Average = a });
			return results.Where(rs => rs.index == lstPrcItems.Count).Select(r => r.Average).FirstOrDefault();
		}

		// Common delegate to interact with user for getting responses
		Action<string, Action<string>> SetUserReponse = (userQuery, setResponse) => 
		{
			Console.WriteLine(userQuery);
			var userResponse = Console.ReadLine();
			if(!string.IsNullOrEmpty(userResponse)) 
			{ 
				setResponse(userResponse);
			}			
		};

		// Price process starting function called from Main
		public void StartProcessAsync()
		{
			// await Task.Run(() => {
				//Get user responses and set mutable variables 
				SetUserReponse("Please enter expected currency pair to get price... (format: CURR1_CURR2(i.e. 'BTC_ETH' is by default)) and then press enter or just press enter to continue with default one",
					(s) => { this._currPair = s; }
				);
				SetUserReponse("Please enter json property/column name to look for price... in API result (format: columnname(i.e. 'last' is by default)) and then press enter or just press enter to continue with default one",
					(s) => { this._priceKey = s; }
				);
				SetUserReponse(string.Format("Please confirm json path as expected in api response is {0}, continue by pressing enter or provide the required one and then press enter", JsonPath),
					(s) => { this._jsonPath = s; }
				);

				Console.WriteLine("Starting with interval of {0} seconds {1}", this._priceInterval.TotalSeconds, Environment.NewLine);

				int Counter = 0;
				var lstPrices = new Dictionary<int,decimal>();
				var aTimer = new System.Timers.Timer();

				aTimer.Elapsed += (s , e) =>
				{
					Counter++;
					aTimer.Interval = this._priceInterval.TotalMilliseconds;
					Console.Write("{0}. Fetching price for {1} at {2} ", Counter, this._currPair, e.SignalTime);
					var taskPrice = Task.Run(async() => { 
						return await this.GetPriceAsync((msg, ex) => 
							{
								if(ex != null) 
								{
									Console.WriteLine(" Error occured : {0}{1}Error details : {2}", msg, Environment.NewLine, ex);
								}
								else
								{
									Console.Write(msg);
									// aTimer.Stop();
									// throw ex;
								}
							}); 
						}
					);
					while(taskPrice.Status != TaskStatus.RanToCompletion)
					{
						Console.Write(".");
						System.Threading.Thread.Sleep(200);
					}

					var returnPrice = taskPrice.Result;

					if(!string.IsNullOrEmpty(returnPrice))
					{
						Console.WriteLine(" and it is {0}", returnPrice);
						lstPrices.Add(lstPrices.Count + 1, decimal.Parse(returnPrice));
						decimal diffPrice = Decimal.Subtract(decimal.Parse(returnPrice), lstPrices.Where(t => t.Key == lstPrices.Count() - 2).Select(r => r.Value).FirstOrDefault());
						
						//Final output in console with all details
						Console.WriteLine("Average price(out of available until now) is : {0} {1} {2} {3}"
											, lstPrices.Select(t => t.Value).Average() 
											, (lstPrices.Count > 1 ? 
												string.Format("with {0} of {1} from previous price"
													, (diffPrice == 0 ? "No Change(==)" : (diffPrice > 0 ? "Increase(+)" : "Decrease(-)"))
													, diffPrice)
												: "") 
											, (SampleLength(lstPrices) > 0 ?
												string.Format("having Simple Moving Average(SMA) of {0}", GetAverage(lstPrices))
												: "")	
											, Environment.NewLine
										);
					}
				};
				aTimer.Start();
				Console.ReadLine();
			//});
    }

		// Function to get prices from POLONIEX API
		// <logStatus type=Action>log or catch errors and pass back handle to caller</logStatus>
		public async Task<string> GetPriceAsync(Action<string, Exception> logStatus = null)
		{
			Action<string, Exception> logText = (msg, ex) => 
			{ 
				if(logStatus != null) logStatus(msg, ex); 
			};

			HttpClient client;
			HttpResponseMessage response;
			string resultPrice = "";
			try
			{
				client = new HttpClient();
				response = new HttpResponseMessage();
				response  = await client.GetAsync(this._url);
				if(response.IsSuccessStatusCode)
				{
					logText(string.Format(" API response is : {0} ", response.StatusCode), null);
					var resJson = await response.Content.ReadAsStringAsync();
					JObject jObj = JObject.Parse(resJson);
					if(jObj.Type.ToString() == "Object")
					{
						resultPrice = jObj.SelectTokens(_jsonPath).DefaultIfEmpty("").Select(s => (string)s).FirstOrDefault();
					}
					else
					{
						logText(string.Format(" Response was not parsable : {0}", resJson), new Exception());
					}
				}
				else
				{
					logText(string.Format(" Response status was : {0}", response.IsSuccessStatusCode), new Exception());
				}
			}
			catch (Exception ex)
			{
				logText(ex.Message, ex);
			}
			finally
			{
				client = null;
				response = null;
			}
			return resultPrice;
		}
	}
}