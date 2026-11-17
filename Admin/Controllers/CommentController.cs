using Admin.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Data.Repositories;
using Entities;
using Entities.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CommentController : Controller
    {
        private readonly IRepository<Comment> CommentRepo;
        private readonly IMapper mapper;

        public CommentController(
            IRepository<Comment> commentRepo,
            IMapper mapper)
        {
            CommentRepo = commentRepo;
            this.mapper = mapper;
        }

        // نمایش تمام نظرات
        public async Task<IActionResult> Index()
        {
            var comments = await CommentRepo.AllTableNoTracking
                .ProjectTo<CommentDto>(mapper.ConfigurationProvider)
                .OrderByDescending(x => x.CreationDateTime)
                .ToListAsync();

            return View(comments);
        }

        // نمایش نظرات در انتظار بررسی
        public async Task<IActionResult> Pending()
        {
            var comments = await CommentRepo.AllTableNoTracking
                .Where(x => x.Status == CommentStatus.Pending)
                .ProjectTo<CommentDto>(mapper.ConfigurationProvider)
                .OrderByDescending(x => x.CreationDateTime)
                .ToListAsync();

            return View(comments);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var comment = await CommentRepo.AllTableNoTracking
                .ProjectTo<CommentDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var comment = await CommentRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            comment.Status = CommentStatus.Approved;

            await CommentRepo.UpdateAsync(comment, HttpContext.RequestAborted);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            var comment = await CommentRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            comment.Status = CommentStatus.Rejected;

            await CommentRepo.UpdateAsync(comment, HttpContext.RequestAborted);

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var comment = await CommentRepo.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (comment == null)
            {
                return NotFound();
            }

            await CommentRepo.DeleteAsync(comment, HttpContext.RequestAborted);

            return RedirectToAction(nameof(Index));
        }
    }
}