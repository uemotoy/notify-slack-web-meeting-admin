using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Documents;

using NotifySlackWebMeetingsAdmin.Api.Entities;
using User = NotifySlackWebMeetingsAdmin.Api.Entities.User;
using NotifySlackWebMeetingsAdmin.Api.Queries;
namespace NotifySlackWebMeetingAdmin.Api
{
  public static class Users
  {


    #region ユーザーを取得する
    [FunctionName("GetUsers")]
    public static async Task<IActionResult> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Users")] HttpRequest req,
        [CosmosDB(
          databaseName: "notify-slack-web-meeting-db",
          collectionName: "Users",
          ConnectionStringSetting = "CosmosDBConnectionString")]DocumentClient client,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");
      string message = string.Empty;

      try
      {
        log.LogInformation("GET SlackChannels");

        // クエリパラメータから検索条件パラメータを設定
        UsersQueryParameter queryParameter = new UsersQueryParameter()
        {
          Ids = req.Query["ids"],
          Name = req.Query["name"],
          EmailAddress = req.Query["emailAddress"],
        };

        // Slackチャンネル情報を取得
        message = JsonConvert.SerializeObject(await GetUsers(client, queryParameter, log));
      }
      catch (Exception ex)
      {
        return new BadRequestObjectResult(ex);
      }

      return new OkObjectResult(message);
    }


    internal static async Task<IEnumerable<User>> GetUsers(
       DocumentClient client,
       UsersQueryParameter queryParameter,
       ILogger log
     )
    {
      Uri collectionUri = UriFactory.CreateDocumentCollectionUri("notify-slack-web-meeting-db", "Users");
      IDocumentQuery<User> query = client.CreateDocumentQuery<User>(collectionUri, new FeedOptions { EnableCrossPartitionQuery = true, PopulateQueryMetrics = true })
          .Where(queryParameter.GetWhereExpression())
          .AsDocumentQuery();
      log.LogInformation(query.ToString());

      var documentItems = new List<User>();
      while (query.HasMoreResults)
      {
        foreach (var documentItem in await query.ExecuteNextAsync<User>())
        {
          documentItems.Add(documentItem);
        }
      }

      return documentItems;
    }
    #endregion

    #region ユーザーを追加する
    /// <summary>
    /// ユーザーを追加する。
    /// </summary>
    /// <returns>追加したユーザー情報</returns>        
    [FunctionName("AddUsers")]
    public static async Task<IActionResult> AddUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Users")] HttpRequest req,
        [CosmosDB(
                databaseName: "notify-slack-web-meeting-db",
                collectionName: "Users",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");
      string message = string.Empty;

      try
      {
        log.LogInformation("POST Users");

        // リクエストのBODYからパラメータ取得
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        // エンティティに設定
        User user = new User()
        {
          Name = data?.name,
          EmailAddress = data?.emailAddress,
        };

        // 入力値チェックを行う
        UserValidator validator = new UserValidator();
        validator.ValidateAndThrow(user);

        // Slackチャンネル情報を登録
        message = await AddUsers(documentsOut, user);
      }
      catch (Exception ex)
      {
        return new BadRequestObjectResult(ex);
      }

      return new OkObjectResult(message);
    }

    /// <summary>
    /// ユーザー情報を登録する。
    /// </summary>
    /// <param name="documentsOut">CosmosDBのドキュメント</param>
    /// <param name="user">ユーザー情報</param>
    /// <returns></returns>
    private static async Task<string> AddUsers(
                IAsyncCollector<dynamic> documentsOut,
                User user
                )
    {
      // 登録日時にUTCでの現在日時を設定
      user.RegisteredAt = DateTime.UtcNow;
      // Add a JSON document to the output container.
      string documentItem = JsonConvert.SerializeObject(user, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
      await documentsOut.AddAsync(documentItem);
      return documentItem;
    }
    #endregion

    #region ユーザーを削除する
    /// <summary>
    /// ユーザー情報を削除する。
    /// </summary>
    /// <param name="req">HTTPリクエスト。</param>
    /// <param name="client">CosmosDBのドキュメントクライアント。</param>
    /// <param name="log">ロガー。</param>
    /// <returns>削除したユーザー情報。</returns>
    [FunctionName("DeleteUserById")]
    public static async Task<IActionResult> DeleteUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Users/{id}")] HttpRequest req,
        [CosmosDB(
                databaseName: "notify-slack-of-web-meeting-db",
                collectionName: "Users",
                ConnectionStringSetting = "CosmosDbConnectionString")
                ]DocumentClient client,
        ILogger log)
    {
      log.LogInformation("C# HTTP trigger function processed a request.");
      string message = string.Empty;

      try
      {
        string id = req.RouteValues["id"].ToString();
        log.LogInformation($"DELETE slackChannels/{id}");

        // ユーザー情報を削除
        var documentItems = await DeleteUserById(client, id, log);

        if (!documentItems.Any())
        {
          return new NotFoundObjectResult($"Target item not found. Id={id}");
        }
        message = JsonConvert.SerializeObject(documentItems);

      }
      catch (Exception ex)
      {
        return new BadRequestObjectResult(ex);
      }

      return new OkObjectResult(message);
    }

    /// <summary>
    /// ユーザー情報を削除する。
    /// </summary>
    /// <param name="client">CosmosDBのドキュメントクライアント。</param>
    /// <param name="ids">削除するユーザー情報のID一覧。</param>
    /// <param name="log">ロガー。</param>
    /// <returns>削除したユーザー情報。</returns>
    private static async Task<IEnumerable<User>> DeleteUserById(
               DocumentClient client,
               string ids,
               ILogger log)
    {
      // 事前に存在確認後に削除

      // クエリパラメータに削除するユーザー情報のIDを設定
      UsersQueryParameter queryParameter = new UsersQueryParameter()
      {
        Ids = ids,
      };

      // ユーザー情報を取得
      var documentItems = await GetUsers(client, queryParameter, log);
      foreach (var documentItem in documentItems)
      {
        // ユーザー情報を削除
        // Delete a JSON document from the container.
        Uri documentUri = UriFactory.CreateDocumentUri("notify-slack-web-meeting-db", "Users", documentItem.Id);
        await client.DeleteDocumentAsync(documentUri, new RequestOptions() { PartitionKey = new PartitionKey(documentItem.Id) });
      }

      return documentItems;
    }
    #endregion
  }
}
