using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanView.ViewModels;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class BannerController : Controller
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IWebHostEnvironment _env;

        public BannerController(BanQuanAu1DbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Banner
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners.ToListAsync();
            return View(banners);
        }

        // GET: Admin/Banner/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Banner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner model, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "img/banner", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    model.ImageUrl = "/img/banner/" + fileName;
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Admin/Banner/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return NotFound();

            return View(banner);
        }

        // POST: Admin/Banner/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner model, IFormFile ImageFile)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var path = Path.Combine(_env.WebRootPath, "img/banner", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    banner.ImageUrl = "/img/banner/" + fileName;
                }

                banner.Title = model.Title;
                banner.Link = model.Link;

                _context.Update(banner);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
