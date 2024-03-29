﻿// This SYNCHRONOUS executer pairs with the Alan Richardson APIs models
// in the scenario where the APIs server is run locally
// from a Java jar file.
//
// HttpClient awaitable methods are called and the Result
// property is assigned to a variable.  It is the assignment
// of the Result that makes the async Task seem to execute
// synchronously.  Internally, the async Task completes
// and then fills in Result.  There are still asynchronous
// things happening, but Result is available.  For example
// methods that want the response body must await that.
// 
// 2021-04-21 Doug Szabo
// for
// Serious Quality LLC

using System;
using System.Text; // Encoding
using System.Diagnostics; // Process, ProcessStartInfo
using System.Threading.Tasks; // Task
using System.Collections.Generic; // List
using System.Net.Http;
using System.Net.Http.Headers;

namespace SynchronousHttpClientExecuter
{
    public class SynchronousHttpClient
    {
        // A process for the APIs server
        private Process process;

        public string responseBody;

        public HttpResponseMessage response;

        // TODO: The following variable should set the client.BaseAddress via 
        // new Uri(), but for now it works to concatenate it with the uri 
        // suffix in method calls.
        public string server = "http://foo.bar";

        private async Task<bool> GetRequestTask(List<string> acceptHeaders, string uri, List<string[]> customHeaders)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }

            foreach (string[] customHeader in customHeaders)
            {
                client.DefaultRequestHeaders.Add(customHeader[0], customHeader[1]);
            }

            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                response = client.GetAsync(server + uri).Result;
//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("Get request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Get request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private async Task<bool> DeleteRequestTask(List<string> acceptHeaders, string uri)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                response = client.DeleteAsync(server + uri).Result;
//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("Delete request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DeleteRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Delete request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private async Task<bool> PostRequestTask(List<string> acceptHeaders, string uri, StringContent body, List<string[]> customHeaders, string usernamePassword = "")
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            foreach (string[] customHeader in customHeaders)
            {
                client.DefaultRequestHeaders.Add(customHeader[0], customHeader[1]);
            }

            if (usernamePassword != "")
            {
                // https://gist.github.com/bryanbarnard/8102915
                var byteArray = Encoding.ASCII.GetBytes(usernamePassword);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            responseBody = String.Empty;

            try
            { 
                response = client.PostAsync(server + uri, body).Result;
//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                { 
                    responseBody = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("Post request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("PostRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Post request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private async Task<bool> PutRequestTask(List<string> acceptHeaders, string uri, StringContent body)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                response = client.PutAsync(server + uri, body).Result;
//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("Put request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            { 
                Console.WriteLine("PutRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Put request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private async Task<bool> PatchRequestTask(List<string> acceptHeaders, string uri, StringContent body)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                response = client.PatchAsync(server + uri, body).Result;
//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
                    responseBody = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine(responseBody);
                }
                else
                {
                    Console.WriteLine("Patch request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("PatchRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Patch request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private bool HeadRequestTask(List<string> acceptHeaders, string uri)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                // per https://stackoverflow.com/questions/16416699/http-head-request-with-httpclient-in-net-4-5-and-c-sharp

                response = client.SendAsync(new HttpRequestMessage(HttpMethod.Head, server + uri), HttpCompletionOption.ResponseHeadersRead).Result;

//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
//                    Console.WriteLine("{0}: {1}", response.StatusCode, response.Headers.ToString());
                }
                else
                {
                    Console.WriteLine("Head request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("HeadRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Head request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private bool TraceRequestTask(List<string> acceptHeaders, string uri)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");

            responseBody = String.Empty;

            try
            {
                // per https://stackoverflow.com/questions/16416699/http-head-request-with-httpclient-in-net-4-5-and-c-sharp

                response = client.SendAsync(new HttpRequestMessage(HttpMethod.Trace, server + uri), HttpCompletionOption.ResponseHeadersRead).Result;

//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
//                    Console.WriteLine("{0}: {1}", response.StatusCode, response.Headers.ToString());
//                    Console.WriteLine("Trailing headers");
//                    Console.WriteLine("{0}", response.TrailingHeaders.ToString());
                }
                else
                {
                    Console.WriteLine("Trace request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("TraceRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Trace request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        private bool OptionsRequestTask(List<string> acceptHeaders, string uri)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();

            foreach (string header in acceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header));
            }
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core test Executer");
         // per https://stackoverflow.com/questions/55767269/how-to-make-an-options-request-with-httpclient
            client.DefaultRequestHeaders.Add("Origin", server);

            responseBody = String.Empty;

            try
            {
                response = client.SendAsync(new HttpRequestMessage(HttpMethod.Options, server + uri), HttpCompletionOption.ResponseHeadersRead).Result;

//                Console.WriteLine(response.ToString());

                if (response.IsSuccessStatusCode)
                {
//                    Console.WriteLine(response.ToString());
                }
                else
                {
                    Console.WriteLine("Options request returned HTTP code {0}", response.StatusCode);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("OptionsRequestTask exception: {0}", e.Message);
                client.Dispose();
                return false;
            }
            // The Options request got an answer.  Look at the HTTP code to
            // understand more.
            client.Dispose();
            return true;
        }

        // Call startup when running an Http server locally.
        // If reaching out to a running web service, do not call this method.
        public bool Startup(string workingDirectory, string fileName, string arguments)
        {
            process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.FileName = fileName;
            startInfo.Arguments = arguments;
            process.StartInfo = startInfo;

            try
            {
                process.Start();
                // Give the process time to get running, otherwise
                // we won't get the first response from the service and
                // we will report a problem - false positive.
                System.Threading.Thread.Sleep(1500);
            }
            catch (Exception e)
            {
                // Tell why we were unable to start the process.
                Console.WriteLine("SynchronousHttpClient.Startup() exception: {1}", e.Message);
                Console.WriteLine("Trying to start {0}", fileName);
                Console.WriteLine("with arguments {0}", arguments);
                Console.WriteLine("at path {0}", workingDirectory);
                return false;
            }

            // Process started.
            return true;
        }

        public bool GetRequest(List<string> acceptHeaders, string uri, List<string[]> customHeaders)
        {
            Task<bool> get = GetRequestTask(acceptHeaders, uri, customHeaders);

            if (get.Status == TaskStatus.RanToCompletion)
            {
                return get.Result;
            }
            else
            {
                Console.WriteLine("GetRequest for {0} did not run to completion.", uri);
                return false;
            }
        }

        public bool PostRequest(List<string> acceptHeaders, string uri, StringContent body, List<string[]> customHeaders, string usernamePassword = "")
        {
            Task<bool> post = PostRequestTask(acceptHeaders, uri, body, customHeaders, usernamePassword);

            if (post.Status == TaskStatus.RanToCompletion)
            {
                return post.Result;
            }
            else
            {
                Console.WriteLine("PostRequest for {0} did not run to completion.", uri);
                return false;
            }
        }

        public bool PutRequest(List<string> acceptHeaders, string uri, StringContent body)
        {
            Task<bool> put = PutRequestTask(acceptHeaders, uri, body);

            if (put.Status == TaskStatus.RanToCompletion)
            {
                return put.Result;
            }
            else
            {
                Console.WriteLine("PutRequest for {0} did not run to completion.", uri);
                return false;
            }
        }

        public bool PatchRequest(List<string> acceptHeaders, string uri, StringContent body)
        {
            Task<bool> patch = PatchRequestTask(acceptHeaders, uri, body);

            if (patch.Status == TaskStatus.RanToCompletion)
            {
                return patch.Result;
            }
            else
            {
                Console.WriteLine("PatchRequest for {0} did not run to completion.", uri);
                return false;
            }
        }

        public bool HeadRequest(List<string> acceptHeaders, string uri)
        {
            bool head = HeadRequestTask(acceptHeaders, uri);
            return head;
        }

        public bool DeleteRequest(List<string> acceptHeaders, string uri)
        {
            Task<bool> deleteTask = DeleteRequestTask(acceptHeaders, uri);

            if (deleteTask.Status == TaskStatus.RanToCompletion)
            {
                return deleteTask.Result;
            }
            else
            {
                Console.WriteLine("DeleteRequest for {0} did not run to completion.", uri);
                return false;
            }
        }

        public bool OptionsRequest(List<string> acceptHeaders, string uri)
        {
            bool options = OptionsRequestTask(acceptHeaders, uri);
            return options;
        }

        public bool TraceRequest(List<string> acceptHeaders, string uri)
        {
            bool taskStatus = TraceRequestTask(acceptHeaders, uri);
            return taskStatus;
        }
        /*
//The data that needs to be sent. Any object works.
	var pocoObject = new 
	{
	  	Name = "John Doe",
		Occupation = "gardener"
	};

  	//Converting the object to a json string. NOTE: Make sure the object doesn't contain circular references.
	string json = JsonConvert.SerializeObject(pocoObject);
        */
    }
}
