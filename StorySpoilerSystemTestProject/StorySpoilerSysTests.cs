using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoilerSystemTestProject.Models;


namespace StorySpoilerSystemTestProject
{
    [TestFixture]

    public class StorySpoilerSysTests
    {
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";
        private const string LoginUsername = "smo1";
        private const string LoginPassword = "12345678";
        private string LastStoryId;

        private RestClient client;


        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = "";
            jwtToken = GetAccessToken(LoginUsername, LoginPassword);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);


        }

        private string GetAccessToken(string username, string password)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });
            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var apiResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
                string token = apiResponse.GetProperty("accessToken").ToString();
                

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Access token is empty or null.");
                }
              return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to get access token. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateNewStorySpoilerWithRequiredFieldsShouldSuccess()
        {
            var storySpoilerStructure = new StoryDTO
            {
                Title = "Test Story Spoiler",
                Description = "This is a test story spoiler.",
                Url = ""
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storySpoilerStructure);
            var response = this.client.Execute(request);

            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            string message = responsed.Message;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 OK.");
            Assert.That(message, Is.EqualTo("Successfully created!"), "Expected success message.");

            LastStoryId = responsed.StoryId;
        }

        [Order(2)]
        [Test]
        public void EditCreatedStorySpoilerShouldSuccess()
        {
            if (string.IsNullOrWhiteSpace(LastStoryId))
            {
                Assert.Fail("LastStoryId is not set. Cannot edit story.");
            }
            var updatedStorySpoiler = new StoryDTO
            {
                Title = "Updated Story Spoiler",
                Description = "This is an updated story spoiler.",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{LastStoryId}", Method.Put);
            request.AddJsonBody(updatedStorySpoiler);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            string message = responsed.Message;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(message, Is.EqualTo("Successfully edited"), "Expected success message.");

        }

        [Order(3)]
        [Test]
        public void GetAllStorySpoilersShouldSuccess()
        {
            var request = new RestRequest("/api/Story/All");
            var response = this.client.Execute(request);

            var responsed = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responsed, Is.Not.Null.Or.Empty, "Expected non-null or empty response.");
        }

        [Order(4)]
        [Test]
        public void DeleteStorySpoilerByIdShouldSuccess()
        {
            var request = new RestRequest($"/api/Story/Delete/{LastStoryId}", Method.Delete);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            string message = responsed.Message;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(message, Is.EqualTo("Deleted successfully!"), "Expected success message.");

        }

        [Order(5)]
        [Test]
        public void StorySpoilerWitoutRequiredFieldsShouldFail()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(new { Title = "", Description = "" });
            var response = this.client.Execute(request);
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");         

        }

        [Order(6)]
        [Test]
        public void EditingNonExistingStorySpoilerShouldFail()
        {
            var request = new RestRequest("/api/Story/Edit/0000", Method.Put);
            request.AddJsonBody(new { Title = "Non-existing Story", Description = "Story does not exist." });
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            string message = responsed.Message;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found.");
            Assert.That(message, Is.EqualTo("No spoilers..."), "Expected 'No spoilers...' message.");

        }

        [Order(7)]
        [Test]
        public void DeletingNonExistingStorySpoilerShouldFail()
        {
            var request = new RestRequest("/api/Story/Delete/0000", Method.Delete);
            var response = this.client.Execute(request);
            var responsed = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            string message = responsed.Message;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(message, Is.EqualTo("Unable to delete this story spoiler!"), "Expected 'Unable to delete this story spoiler!' message.");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }


    }
}