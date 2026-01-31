using EasyLingo.Data;
using EasyLingo.Data.Entities;
using EasyLingo.Data.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyLingo.Services.Interfaces;
using EasyLingo.Domain.Exceptions;

namespace EasyLingo.Services
{
    public class DataService : IDataService
    {
        private readonly AppDbContext _db;

        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Set> _setRepo;
        private readonly IRepository<Term> _termRepo;
        private readonly IRepository<UserSetCategory> _catRepo;
        private readonly IRepository<UserSetProgress> _progressRepo;
        private readonly IRepository<UserTermStatus> _statusRepo;


        public DataService()
        {
            _db = new AppDbContext();

            _userRepo = new EfRepository<User>(_db);
            _setRepo = new EfRepository<Set>(_db);
            _termRepo = new EfRepository<Term>(_db);
            _catRepo = new EfRepository<UserSetCategory>(_db);
            _progressRepo = new EfRepository<UserSetProgress>(_db);
            _statusRepo = new EfRepository<UserTermStatus>(_db);
        }

        // USERS
        public async Task<User> AddUserAsync(string username, string passwordHash)
        {
            var user = new User { Username = username, PasswordHash = passwordHash };
            await _userRepo.AddAsync(user);

            //przy dodaniu nowego usera - dodajemy zawsze początkowe wartości - zestawy, kategorie, słówka
            var catBasic = new UserSetCategory { UserId = user.UserId, Name = "Podstawowe" };
            var catDaily = new UserSetCategory { UserId = user.UserId, Name = "Codzienne" };
            _db.UserSetCategories.AddRange(catBasic, catDaily);
            await _db.SaveChangesAsync();

            var animals = new Set
            {
                Name = "Zwierzęta",
                Description = "Podstawowe zwierzęta",
                LangId = 1,
                UserId = user.UserId,
                SetCategoryId = catBasic.SetCategoryId
            };

            var verbs = new Set
            {
                Name = "Czasowniki podstawowe",
                Description = "Najważniejsze czasowniki",
                LangId = 1,
                UserId = user.UserId,
                SetCategoryId = catDaily.SetCategoryId
            };

            _db.Sets.AddRange(animals, verbs);
            await _db.SaveChangesAsync();

            _db.UserSetProgresses.AddRange(
                new UserSetProgress { UserId = user.UserId, SetId = animals.SetId, ProgressPercent = 0 },
                new UserSetProgress { UserId = user.UserId, SetId = verbs.SetId, ProgressPercent = 0 }
            );
            await _db.SaveChangesAsync();

            var t1 = new Term { TermName = "Dog", Definition = "Pies", SetId = animals.SetId };
            var t2 = new Term { TermName = "Cat", Definition = "Kot", SetId = animals.SetId };

            var t3 = new Term { TermName = "Run", Definition = "Biegać", SetId = verbs.SetId };
            var t4 = new Term { TermName = "Eat", Definition = "Jeść", SetId = verbs.SetId };

            _db.Terms.AddRange(t1, t2, t3, t4);
            await _db.SaveChangesAsync();

            _db.UserTermStatuses.AddRange(
                new UserTermStatus { UserId = user.UserId, TermId = t1.TermId, Status = 0 },
                new UserTermStatus { UserId = user.UserId, TermId = t2.TermId, Status = 0 },
                new UserTermStatus { UserId = user.UserId, TermId = t3.TermId, Status = 0 },
                new UserTermStatus { UserId = user.UserId, TermId = t4.TermId, Status = 0 }
            );
            await _db.SaveChangesAsync();

            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            await _userRepo.UpdateAsync(user);
            return user;
        }

        public async Task DeleteUserAsync(User user)
        {
            var progresses = _db.UserSetProgresses.Where(p => p.UserId == user.UserId);
            var statuses = _db.UserTermStatuses.Where(s => s.UserId == user.UserId);

            var sets = _db.Sets.Where(s => s.UserId == user.UserId);
            var setIds = await sets.Select(s => s.SetId).ToListAsync();

            var terms = _db.Terms.Where(t => setIds.Contains(t.SetId));
            var categories = _db.UserSetCategories.Where(c => c.UserId == user.UserId);

            _db.UserTermStatuses.RemoveRange(statuses);
            _db.Terms.RemoveRange(terms);
            _db.UserSetProgresses.RemoveRange(progresses);
            _db.Sets.RemoveRange(sets);
            _db.UserSetCategories.RemoveRange(categories);

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> ValidateUserAsync(string username, string passwordHash)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == passwordHash);
        }

        // SETS
        public async Task<Set> AddSetAsync(int userId, string name, string description, int langId, string? categoryName)
        {
            int? categoryId = await GetOrCreateSetCategoryIdAsync(userId, categoryName);

            var set = new Set
            {
                Name = name,
                Description = description,
                LangId = langId,
                UserId = userId,
                SetCategoryId = categoryId
            };

            await _setRepo.AddAsync(set); // generyk


            _db.UserSetProgresses.Add(new UserSetProgress
            {
                UserId = userId,
                SetId = set.SetId,
                ProgressPercent = 0
            });

            await _db.SaveChangesAsync();
            return set;
        }


        public async Task<Set?> GetSetByIdAsync(int setId)
        {
            return await _db.Sets
                .Include(s => s.Language)
                .Include(s => s.SetCategory)
                .FirstOrDefaultAsync(s => s.SetId == setId);
        }

        public async Task<List<Set>> GetAllSetsAsync()
        {
            return await _db.Sets
                .Include(s => s.Language)
                .Include(s => s.SetCategory)
                .ToListAsync();
        }

        public async Task<Set> UpdateSetAsync(Set set)
        {
            await _setRepo.UpdateAsync(set);
            return set;
        }

        public async Task DeleteSetAsync(Set set)
        {
            var progresses = _db.UserSetProgresses.Where(p => p.SetId == set.SetId).ToList();
            var terms = _db.Terms.Where(t => t.SetId == set.SetId).ToList();

            foreach (var term in terms)
                await DeleteTermAsync(term);

            _db.UserSetProgresses.RemoveRange(progresses);
            _db.Sets.Remove(set);
            await _db.SaveChangesAsync();
        }

        // TERMS

        public async Task<Term> AddTermAsync(string termWord, string definition, int setId)
        {
            var set = await _db.Sets.FirstOrDefaultAsync(s => s.SetId == setId);
            if (set == null) throw new NotFoundException("Nie znaleziono zestawu.");


            var term = new Term
            {
                TermName = termWord,
                Definition = definition,
                SetId = setId
            };

            await _termRepo.AddAsync(term);

            _db.UserTermStatuses.Add(new UserTermStatus
            {
                UserId = set.UserId,
                TermId = term.TermId,
                Status = 0
            });

            await _db.SaveChangesAsync();
            return term;
        }

        public async Task<Term?> GetTermByIdAsync(int termId)
        {
            return await _db.Terms
                .Include(t => t.Set)
                .FirstOrDefaultAsync(t => t.TermId == termId);
        }

        public async Task<List<Term>> GetTermsBySetAsync(int setId)
        {
            return await _db.Terms
                .Where(t => t.SetId == setId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Term> UpdateTermAsync(Term term)
        {
            await _termRepo.UpdateAsync(term);
            return term;
        }

        public async Task DeleteTermAsync(Term term)
        {
            var statuses = _db.UserTermStatuses.Where(s => s.TermId == term.TermId);
            _db.UserTermStatuses.RemoveRange(statuses);

            _db.Terms.Remove(term);
            await _db.SaveChangesAsync();
        }

        // USER SET PROGRESS

        public async Task<UserSetProgress?> GetProgressAsync(int userId, int setId)
        {
            return await _db.UserSetProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);
        }

        public async Task UpdateProgressAsync(int userId, int setId, int progressPercent)
        {
            var progress = await _db.UserSetProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);
            if (progress == null)
            {
                progress = new UserSetProgress
                {
                    UserId = userId,
                    SetId = setId,
                    ProgressPercent = progressPercent
                };
                _db.UserSetProgresses.Add(progress);
            }
            else
            {
                progress.ProgressPercent = progressPercent;
                _db.UserSetProgresses.Update(progress);
            }
            await _db.SaveChangesAsync();
        }

        public async Task RecalculateProgressAsync(int userId, int setId)
        {
            var total = await _db.Terms.CountAsync(t => t.SetId == setId);

            var known = await _db.UserTermStatuses
                .Include(s => s.Term)
                .CountAsync(s => s.UserId == userId && s.Term.SetId == setId && s.Status == 1);

            var progressPercent = total == 0 ? 0 : (known * 100) / total;
            await UpdateProgressAsync(userId, setId, progressPercent);
        }

        // USER SET CATEGORY
        public async Task<UserSetCategory> AddSetCategoryAsync(int userId, string name)
        {
            var category = new UserSetCategory { UserId = userId, Name = name };
            _db.UserSetCategories.Add(category);
            await _db.SaveChangesAsync();
            return category;
        }

        public async Task<UserSetCategory?> GetSetCategoryByIdAsync(int setCategoryId)
        {
            return await _db.UserSetCategories.FirstOrDefaultAsync(c => c.SetCategoryId == setCategoryId);
        }

        public async Task<List<UserSetCategory>> GetSetCategoriesByUserAsync(int userId)
        {
            return await _db.UserSetCategories
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<int?> GetOrCreateSetCategoryIdAsync(int userId, string? categoryName)
        {
            categoryName = (categoryName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var existing = await _db.UserSetCategories
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Name.ToLower() == categoryName.ToLower());

            if (existing != null)
                return existing.SetCategoryId;

            var created = new UserSetCategory
            {
                UserId = userId,
                Name = categoryName
            };
            _db.UserSetCategories.Add(created);
            await _db.SaveChangesAsync();
            return created.SetCategoryId;
        }


        public async Task UpdateSetCategoryAsync(UserSetCategory category)
        {
            _db.UserSetCategories.Update(category);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteSetCategoryAsync(UserSetCategory category)
        {
            // opcjonalnie: przed usunięciem odpinamy kategorię od setów usera
            var sets = await _db.Sets.Where(s => s.UserId == category.UserId && s.SetCategoryId == category.SetCategoryId).ToListAsync();
            foreach (var s in sets) s.SetCategoryId = null;

            _db.UserSetCategories.Remove(category);
            await _db.SaveChangesAsync();
        }

        // USER TERM STATUS
        public async Task<UserTermStatus?> GetStatusAsync(int userId, int termId)
        {
            return await _db.UserTermStatuses.FirstOrDefaultAsync(s => s.UserId == userId && s.TermId == termId);
        }

        public async Task<List<UserTermStatus>> GetStatusesByUserAsync(int userId)
        {
            return await _db.UserTermStatuses.Where(s => s.UserId == userId).ToListAsync();
        }

        public async Task UpdateStatusAsync(int userId, int termId, int statusValue)
        {
            var status = await _db.UserTermStatuses.FirstOrDefaultAsync(s => s.UserId == userId && s.TermId == termId);
            if (status == null)
            {
                status = new UserTermStatus
                {
                    UserId = userId,
                    TermId = termId,
                    Status = statusValue
                };
                _db.UserTermStatuses.Add(status);
            }
            else
            {
                status.Status = statusValue;
                _db.UserTermStatuses.Update(status);
            }
            await _db.SaveChangesAsync();
        }

        // DASHBOARD i SETS LISTS
        public async Task<List<SetProgressDto>> GetUserSetProgressForDashboardAsync(int userId)
        {
            return await _db.UserSetProgresses
                .Where(p => p.UserId == userId)
                .Include(p => p.Set)
                .OrderBy(p => p.SetId)
                .Select(p => new SetProgressDto
                {
                    Name = p.Set.Name,
                    ProgressPercent = p.ProgressPercent
                })
                .Take(3)
                .ToListAsync();
        }

        public async Task<List<(int SetId, string Name, int ProgressPercent, string? CategoryName)>>
            GetUserSetProgressCardsByLangAsync(int userId, int langId, int? categoryId)
            {
                var q = _db.UserSetProgresses
                    .Where(p => p.UserId == userId)
                    .Include(p => p.Set)
                        .ThenInclude(s => s.SetCategory)
                    .Where(p => p.Set.LangId == langId && p.Set.UserId == userId)
                    .AsNoTracking();

                if (categoryId.HasValue)
                    q = q.Where(p => p.Set.SetCategoryId == categoryId.Value);

                var data = await q
                    .OrderBy(p => p.Set.Name)
                    .Select(p => new
                    {
                        p.SetId,
                        p.Set.Name,
                        p.ProgressPercent,
                        CategoryName = p.Set.SetCategory != null ? p.Set.SetCategory.Name : null
                    })
                    .ToListAsync();

                return data.Select(x => (x.SetId, x.Name, x.ProgressPercent, x.CategoryName)).ToList();
            }

        public async Task<List<(int SetId, string Name, int ProgressPercent)>> GetUserSetProgressCardsByLangAsync(int userId, int langId)
        {
            return await _db.UserSetProgresses
                .Where(p => p.UserId == userId && p.Set.LangId == langId && p.Set.UserId == userId)
                .Include(p => p.Set)
                .OrderBy(p => p.Set.Name)
                .Select(p => new { p.SetId, p.Set.Name, p.ProgressPercent })
                .AsNoTracking()
                .ToListAsync()
                .ContinueWith(t => t.Result.Select(x => (x.SetId, x.Name, x.ProgressPercent)).ToList());
        }

        public async Task<(int OverallProgressPercent, int CompletedSets, int TotalSets)> GetDashboardStatsAsync(int userId, int langId)
        {
            var setIds = await _db.Sets
                .Where(s => s.UserId == userId && s.LangId == langId)
                .Select(s => s.SetId)
                .ToListAsync();

            int totalSets = setIds.Count;

            var progresses = await _db.UserSetProgresses
                .Where(p => p.UserId == userId && setIds.Contains(p.SetId))
                .Select(p => p.ProgressPercent)
                .ToListAsync();

            int completed = progresses.Count(p => p >= 100);
            int avg = progresses.Count == 0 ? 0 : (int)Math.Round(progresses.Average());

            return (avg, completed, totalSets);
        }

        public async Task<List<(int SetId, string Name, int ProgressPercent)>> GetContinueSetsForDashboardAsync(int userId, int langId, int take = 3)
        {
            return await _db.UserSetProgresses
                .Include(p => p.Set)
                .Where(p => p.UserId == userId && p.Set.LangId == langId && p.Set.UserId == userId)
                .OrderBy(p => p.ProgressPercent)
                .ThenBy(p => p.Set.Name)
                .Select(p => new { p.Set.SetId, p.Set.Name, p.ProgressPercent })
                .AsNoTracking()
                .Take(take)
                .ToListAsync()
                .ContinueWith(t => t.Result.Select(x => (x.SetId, x.Name, x.ProgressPercent)).ToList());
        }

        public async Task ResetSetProgressAsync(int userId, int setId)
        {
            var termIds = await _db.Terms
                .Where(t => t.SetId == setId)
                .Select(t => t.TermId)
                .ToListAsync();

            var statuses = await _db.UserTermStatuses
                .Where(s => s.UserId == userId && termIds.Contains(s.TermId))
                .ToListAsync();

            foreach (var s in statuses)
                s.Status = 0;

            var progress = await _db.UserSetProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SetId == setId);

            if (progress == null)
            {
                _db.UserSetProgresses.Add(new UserSetProgress
                {
                    UserId = userId,
                    SetId = setId,
                    ProgressPercent = 0
                });
            }
            else
            {
                progress.ProgressPercent = 0;
            }

            await _db.SaveChangesAsync();
        }

    }
}
