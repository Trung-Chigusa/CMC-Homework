namespace CmcHomework.Api.Models;

// Format lỗi chung của API.
// Khi input sai, handler trả object này để JSON lỗi luôn có dạng dễ hiểu:
// { "message": "..." }
public sealed record ApiError(string Message);
