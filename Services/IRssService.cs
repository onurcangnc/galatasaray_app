using RssNewsApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RssNewsApp.Services
{
    public interface IRssService
    {
        Task<List<RssItem>> GetNewsAsync();
        Task<List<RssItem>> GetTransferNewsAsync();
        Task UpdateNewsAsync();
        Task UpdateTransferNewsAsync();
    }
} 