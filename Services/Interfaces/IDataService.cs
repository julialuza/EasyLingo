using EasyLingo.Data.Entities;
using EasyLingo.Data.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyLingo.Services.Interfaces
{
    public interface IDataService
    {
        Task<User> AddUserAsync(string username, string passwordHash);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> UpdateUserAsync(User user);
        Task DeleteUserAsync(User user);
        Task<User?> ValidateUserAsync(string username, string passwordHash);

        Task<Set> AddSetAsync(int userId, string name, string description, int langId, string? categoryName);
        Task<Set?> GetSetByIdAsync(int setId);
        Task<List<Set>> GetAllSetsAsync();
        Task<Set> UpdateSetAsync(Set set);
        Task DeleteSetAsync(Set set);

        Task<Term> AddTermAsync(string termWord, string definition, int setId);
        Task<Term?> GetTermByIdAsync(int termId);
        Task<List<Term>> GetTermsBySetAsync(int setId);
        Task<Term> UpdateTermAsync(Term term);
        Task DeleteTermAsync(Term term);

        Task<UserSetProgress?> GetProgressAsync(int userId, int setId);
        Task UpdateProgressAsync(int userId, int setId, int progressPercent);
        Task RecalculateProgressAsync(int userId, int setId);

        Task<UserSetCategory> AddSetCategoryAsync(int userId, string name);
        Task<UserSetCategory?> GetSetCategoryByIdAsync(int setCategoryId);
        Task<List<UserSetCategory>> GetSetCategoriesByUserAsync(int userId);
        Task UpdateSetCategoryAsync(UserSetCategory category);
        Task DeleteSetCategoryAsync(UserSetCategory category);

        Task<UserTermStatus?> GetStatusAsync(int userId, int termId);
        Task<List<UserTermStatus>> GetStatusesByUserAsync(int userId);
        Task UpdateStatusAsync(int userId, int termId, int statusValue);

        Task<List<SetProgressDto>> GetUserSetProgressForDashboardAsync(int userId);

        Task<List<(int SetId, string Name, int ProgressPercent, string? CategoryName)>> GetUserSetProgressCardsByLangAsync(int userId, int langId, int? categoryId);
        Task<List<(int SetId, string Name, int ProgressPercent)>> GetUserSetProgressCardsByLangAsync(int userId, int langId);

        Task<(int OverallProgressPercent, int CompletedSets, int TotalSets)> GetDashboardStatsAsync(int userId, int langId);
        Task<List<(int SetId, string Name, int ProgressPercent)>> GetContinueSetsForDashboardAsync(int userId, int langId, int take = 3);
    }
}
