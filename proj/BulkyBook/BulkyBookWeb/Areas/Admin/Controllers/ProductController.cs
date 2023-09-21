using System;
using System.IO;
using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.Irepository;
using BulkyBook.DataAccess.Repository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;

namespace BulkyBookWeb.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitofwork;
    private readonly IWebHostEnvironment _hostEnvironment;

    public ProductController(IUnitOfWork unitofwork, IWebHostEnvironment hostEnvironment)
    {
        _unitofwork = unitofwork;
        _hostEnvironment = hostEnvironment;
    }

    public IActionResult Index()
    {
        return View();
    }

    // GET
    public IActionResult Upsert(int? id)
    {
        ProductMV productMV = new()
        {
            product = new(),
            CategoryList = _unitofwork.Category.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            }),
            CoverTypeList = _unitofwork.CoverType.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };
        if (id == null || id == 0)
        {
            //Create Product
            //ViewBag.CategoryList = CategoryList;
            //ViewData["CoverTypeList"] = CoverTypeList;
            return View(productMV);
        }
        else
        {
            productMV.product=_unitofwork.Product.GetFirstOrDefault(u=>u.Id==id);
            return View(productMV);
            //Update Product
        }

        
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(ProductMV obj, IFormFile? file)
    {
        if (ModelState.IsValid)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"Images/Products");
                var extension = Path.GetExtension(file.FileName);
                if (obj.product.ImgeUrl != null)
                {
                    var oldTmagePath = Path.Combine(wwwRootPath, obj.product.ImgeUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldTmagePath))
                    {
                        System.IO.File.Delete(oldTmagePath);
                    }
                }
                using (var fileStreams = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                {
                    file.CopyTo(fileStreams);
                }
                obj.product.ImgeUrl = @"\Images\Products\" + fileName + extension;
            }
            if (obj.product.Id == 0)
            {
                _unitofwork.Product.Add(obj.product);
            }
            else
            {
                _unitofwork.Product.Update(obj.product);
            }

            _unitofwork.save();
            TempData["success"] = "Product created successfully";
            return RedirectToAction("Index");
        }

        return View(obj);
    }

    #region API CALLS
    [HttpGet]
    public IActionResult GetAll()
    {
        var productList=_unitofwork.Product.GetAll(includeProperties:"Category,CoverType");
        return Json(new {data = productList});
    }
    // POST
    [HttpDelete]
    [ValidateAntiForgeryToken]

    public IActionResult Delete(int id)
    {
        var obj = _unitofwork.Product.GetFirstOrDefault(u => u.Id == id);
        if (obj == null)
        {
            return Json(new { success=false,message="Error while deleting"});
        }
        var oldTmagePath = Path.Combine(_hostEnvironment.WebRootPath, obj.ImgeUrl.TrimStart('\\'));
        if (System.IO.File.Exists(oldTmagePath))
        {
            System.IO.File.Delete(oldTmagePath);
        }
        _unitofwork.Product.Remove(obj);
        _unitofwork.save();
        TempData["success"] = "Product deleted successfully";
        return Json(new { success = true, message = "Delete Successful" });
    }
    #endregion
}

