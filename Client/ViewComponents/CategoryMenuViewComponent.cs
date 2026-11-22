using AutoMapper;
using AutoMapper.QueryableExtensions;
using Client.Models;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

using System.Linq;



public class CategoryMenuViewComponent : ViewComponent
{
    private readonly IRepository<Category> CategoryRepo;
    private readonly IMapper mapper;

    public CategoryMenuViewComponent(
        IRepository<Category> categoryRepo,
        IMapper mapper)
    {
        CategoryRepo = categoryRepo;
        this.mapper = mapper;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await CategoryRepo.TableNoTracking
            .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
            .OrderBy(x => x.Order)
            .ToListAsync();

        var rootCategories = categories
            .Where(x => x.ParentId == null)
            .ToList();

        foreach (var category in rootCategories)
        {
            LoadChildren(category, categories);
        }

        return View(rootCategories);
    }

    private void LoadChildren(
        CategoryDto category,
        List<CategoryDto> categories)
    {
        category.Children = categories
            .Where(x => x.ParentId == category.Id)
            .OrderBy(x => x.Order)
            .ToList();

        foreach (var child in category.Children)
        {
            LoadChildren(child, categories);
        }
    }
}