// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TodoListService.Models;

namespace TodoListClient.Services;

/// <summary></summary>
/// <seealso cref="TodoListClient.Services.ITodoListService" />
public class TodoListService : ITodoListService
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly HttpClient _httpClient;
    private readonly string _TodoListScope = string.Empty;
    private readonly string _TodoListBaseAddress = string.Empty;
    private readonly ITokenAcquisition _tokenAcquisition;

    public TodoListService(ITokenAcquisition tokenAcquisition, HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor contextAccessor)
    {
        _httpClient = httpClient;
        _tokenAcquisition = tokenAcquisition;
        _contextAccessor = contextAccessor;
        _TodoListScope = configuration["TodoList:TodoListScope"];
        _TodoListBaseAddress = configuration["TodoList:TodoListBaseAddress"];
    }

    public async Task<Todo> AddAsync(Todo todo)
    {
        await PrepareAuthenticatedClient();

        var jsonRequest = JsonSerializer.Serialize(todo);
        var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await this._httpClient.PostAsync($"{ _TodoListBaseAddress}/api/todolist", jsoncontent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            todo = JsonSerializer.Deserialize<Todo>(content);

            return todo;
        }
         
        throw new WebApiMsalUiRequiredException($"Unexpected status code in the HttpResponseMessage: {response.StatusCode}.", response);

    }

    public async Task DeleteAsync(int id)
    {
        await PrepareAuthenticatedClient();

        var response = await _httpClient.DeleteAsync($"{ _TodoListBaseAddress}/api/todolist/{id}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return;
        }
        throw new WebApiMsalUiRequiredException($"Unexpected status code in the HttpResponseMessage: {response.StatusCode}.", response);
    }

    public async Task<Todo> EditAsync(Todo todo)
    {
        await PrepareAuthenticatedClient();

        var jsonRequest = JsonSerializer.Serialize(todo);
        var jsoncontent = new StringContent(jsonRequest, Encoding.UTF8, "application/json-patch+json");
        var response = await _httpClient.PatchAsync($"{ _TodoListBaseAddress}/api/todolist/{todo.Id}", jsoncontent);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            todo = JsonSerializer.Deserialize<Todo>(content);

            return todo;
        }

        throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
    }

    public async Task<IEnumerable<Todo>> GetAsync()
    {
        await PrepareAuthenticatedClient();
        var response = await _httpClient.GetAsync($"{ _TodoListBaseAddress}/api/todolist");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var todolist = JsonSerializer.Deserialize<IEnumerable<Todo>>(content);

            return todolist;
        }
        throw new HttpRequestException($"Invalid status code in the HttpResponseMessage: {response.StatusCode}.");
    }

    private async Task PrepareAuthenticatedClient()
    {
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { _TodoListScope });
        Debug.WriteLine($"access token-{accessToken}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<Todo> GetAsync(int id)
    {
        await PrepareAuthenticatedClient();
        var response = await _httpClient.GetAsync($"{ _TodoListBaseAddress}/api/todolist/{id}");
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Todo todo = JsonSerializer.Deserialize<Todo>(content);

            return todo;
        }
         
        throw new WebApiMsalUiRequiredException($"Unexpected status code in the HttpResponseMessage: {response.StatusCode}.", response);

    }
}