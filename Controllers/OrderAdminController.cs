﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Nwazet.Commerce.Models;
using Nwazet.Commerce.Services;
using Nwazet.Commerce.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Contents;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Nwazet.Commerce.Controllers {
    [Admin]
    [OrchardFeature("Nwazet.Commerce")]
    public class OrderAdminController : Controller {
        private readonly IOrderService _orderService;
        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        private readonly ITransactionManager _transactionManager;
        private readonly IOrchardServices _orchardServices;

        private dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public OrderAdminController(
            IOrderService orderService,
            IContentManager contentManager,
            IShapeFactory shapeFactory,
            ISiteService siteService,
            ITransactionManager transactionManager,
            IOrchardServices orchardServices) {

            _orderService = orderService;
            _contentManager = contentManager;
            Shape = shapeFactory;
            _siteService = siteService;
            _transactionManager = transactionManager;
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public ActionResult List(ListOrdersViewModel model, PagerParameters pagerParameters) {
            var pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);
            var query = _contentManager.Query<OrderPart, OrderPartRecord>(VersionOptions.Latest);
            var states = OrderPart.States.ToList();

            if (model.Options != null) {
                if (!string.IsNullOrWhiteSpace(model.Options.Search)) {
                    int id;
                    query = int.TryParse(model.Options.Search, out id) ?
                        query.Where(o => o.Id == id) :
                        query.Where(o => o.Customer.Contains(model.Options.Search));
                }
                var filterOption = model.Options.SelectedFilter;
                if (!string.IsNullOrWhiteSpace(filterOption)) {

                    if (!states.Contains(filterOption))
                        return HttpNotFound();

                    query = query.Where(o => o.Status == filterOption);
                }
                else {
                    query = query.Where(o => o.Status != OrderPart.Archived);
                }

                switch (model.Options.OrderBy) {
                    case ContentsOrder.Modified:
                        query.OrderByDescending<CommonPartRecord>(cr => cr.ModifiedUtc);
                        break;
                    case ContentsOrder.Created:
                        query.OrderByDescending<CommonPartRecord>(cr => cr.CreatedUtc);
                        break;
                }
                model.Options.FilterOptions =
                    _orderService.StatusLabels.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value.Text));
            }

            var pagerShape = Shape.Pager(pager).TotalItemCount(query.Count());
            var pageOfContentItems = query.Slice(pager.GetStartIndex(), pager.PageSize).ToList();

            var list = Shape.List();
            list.AddRange(pageOfContentItems.Select(ci => _contentManager.BuildDisplay(ci, "SummaryAdmin")));

            dynamic viewModel = Shape.ViewModel()
                .ContentItems(list)
                .Pager(pagerShape)
                .Options(model.Options);

            return View((object) viewModel);
        }

        [HttpPost]
        [ActionName("List")]
        [Orchard.Mvc.FormValueRequired("submit.Filter")]
        public ActionResult ListFilterPost(ContentOptions options) {
            var routeValues = ControllerContext.RouteData.Values;
            if (options != null) {
                routeValues["Options.OrderBy"] = options.OrderBy;
                if (OrderPart.States.Any(
                        ctd => string.Equals(ctd, options.SelectedFilter, StringComparison.OrdinalIgnoreCase))) {
                    routeValues["Options.SelectedFilter"] = options.SelectedFilter;
                }
                else {
                    routeValues.Remove("Options.SelectedFilter");
                }
                if (String.IsNullOrWhiteSpace(options.Search)) {
                    routeValues.Remove("Options.Search");
                }
                else {
                routeValues["Options.Search"] = options.Search;
                }
            }

            return RedirectToAction("List", routeValues);
        }

        [HttpPost]
        [ActionName("List")]
        [Orchard.Mvc.FormValueRequired("submit.BulkEdit")]
        public ActionResult ListPost(ContentOptions options, IEnumerable<int> itemIds, string returnUrl) {
            if (itemIds != null) {
                var checkedOrders = _contentManager.GetMany<OrderPart>(itemIds, VersionOptions.Latest,
                    QueryHints.Empty);
                switch (options.BulkAction) {
                    case ContentsBulkAction.None:
                        break;
                    case ContentsBulkAction.Remove:
                        foreach (var order in checkedOrders) {
                            if (
                                !_orchardServices.Authorizer.Authorize(Permissions.DeleteContent, order,
                                    T("Couldn't archive selected orders."))) {
                                _transactionManager.Cancel();
                                return new HttpUnauthorizedResult();
                            }

                            order.Status = OrderPart.Archived;
                        }
                        _orchardServices.Notifier.Information(T("Orders successfully archived."));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return this.RedirectLocal(returnUrl, () => RedirectToAction("List"));
        }

        public ActionResult AddEvent(int orderId, string category, string description) {
            var order = _orderService.Get(orderId);
            var orderEvent = order.LogActivity(category, description);
            var result = new JsonResult {
                Data = new {
                    Date = orderEvent.Date.ToLocalTime().ToString(CultureInfo.CurrentCulture),
                    orderEvent.Category,
                    orderEvent.Description
                }
            };
            return result;
        }
    }
}