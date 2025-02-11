﻿using System;
using System.Linq;
using UIOMatic.Extensions;
using UIOMatic.Services;
using UIOMatic.Attributes;
using UIOMatic.Enums;
using UIOMatic.Interfaces;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Trees;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Extensions;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Web.Common.ModelBinders;

namespace UIOMatic.Web.Controllers
{
    //[UmbracoTreeAuthorize("uiomatic")]
    [Tree(Constants.SectionAlias, Constants.TreeAlias, TreeTitle = "UI-O-Matic", SortOrder = 1)]
    [PluginController("UIOMatic")]
    public class UIOMaticTreeController : TreeController, ISearchableTree
    {
        private IUIOMaticObjectService _service;
        private readonly IUIOMaticHelper Helper;


        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;

        public UIOMaticTreeController(ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IMenuItemCollectionFactory menuItemCollectionFactory,
            IEventAggregator eventAggregator,
            IUIOMaticHelper helper)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            _service = UIOMaticObjectService.Instance;
            _menuItemCollectionFactory = menuItemCollectionFactory ?? throw new ArgumentNullException(nameof(menuItemCollectionFactory));
            Helper = helper;
        }


        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var rootResult = base.CreateRootNode(queryStrings);
            if (!(rootResult.Result is null))
            {
                return rootResult;
            }

            var root = rootResult.Value;

            root.Path = $"{Constants.SectionAlias}/{Constants.TreeAlias}/";
            root.Icon = "icon-wand";
            root.HasChildren = true;
            root.MenuUrl = null;

            return root;
        }

        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            var nodes = new TreeNodeCollection();
            var types = Helper.GetUIOMaticFolderTypes().OrderBy(x => x.GetCustomAttribute<UIOMaticFolderAttribute>(false).Order);

            foreach (var type in types)
            {
                var attri = (UIOMaticFolderAttribute)Attribute.GetCustomAttribute(type, typeof(UIOMaticFolderAttribute));
                if (attri == null)
                    continue;

                var alias = attri.Alias.IsNullOrWhiteSpace() ? type.Name : attri.Alias;
                if (attri.ParentAlias == id)
                {
                    var attri2 = attri as UIOMaticAttribute;
                    if (attri2 != null)
                    {
                        if (attri2.HideFromTree)
                            continue;

                        // UIOMatic node
                        if (attri2.RenderType == UIOMaticRenderType.Tree)
                        {
                            // Tree node
                            var node = this.CreateTreeNode(
                                alias,
                                id,
                                queryStrings,
                                attri.FolderName,
                                attri.FolderIcon,
                                true,
                                "uiomatic");

                            nodes.Add(node);
                        }
                        else
                        {
                            // List view node
                            var node = this.CreateTreeNode(
                                alias,
                                id,
                                queryStrings,
                                attri.FolderName,
                                attri.FolderIcon,
                                false,
                                "uiomatic/uiomatic/list/" + alias);

                            node.SetContainerStyle();

                            nodes.Add(node);
                        }
                    }
                    else
                    {
                        // Just a folder
                        var node = this.CreateTreeNode(
                               attri.Alias,
                               id,
                               queryStrings,
                               attri.FolderName,
                               attri.FolderIcon,
                               true,
                               "uiomatic");

                        nodes.Add(node);
                    }
                }
                else if (id == alias)
                {
                    var attri2 = attri as UIOMaticAttribute;
                    if (attri2 != null)
                    {
                        if (attri2.HideFromTree)
                            continue;

                        var primaryKeyPropertyName = type.GetPrimaryKeyName();

                        if (attri2.RenderType == UIOMaticRenderType.Tree)
                        {
                            // List nodes
                            foreach (dynamic item in _service.GetAll(type, attri2.SortColumn, attri2.SortOrder))
                            {
                                var node = CreateTreeNode(
                                    ((object)item).GetPropertyValue(primaryKeyPropertyName) + "?ta=" + id,
                                    id,
                                    queryStrings,
                                    item.ToString(),
                                    attri2.ItemIcon,
                                    false);

                                nodes.Add(node);
                            }
                        }
                    }
                }
            }

            return nodes;
        }

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {

            {
                var menu = _menuItemCollectionFactory.Create();

                //var createText = "Create"; //Services.TextService.Localize("actions/" + ActionNew.Instance.Alias);
                // var deleteText = "Delete"; // Services.TextService.Localize("actions/" + ActionDelete.Instance.Alias);
                var refreshText = "Refresh";// Services.TextService.Localize("actions/" + ActionRefresh.Instance.Alias);

                if (id == "-1")
                {
                    menu.Items.Add(new RefreshNode(refreshText, true));
                }
                else
                {
                    if (id.IndexOf("?") > 0)
                    {
                        var typeAlias = id.Split('?')[1].Replace("ta=", "");
                        var type = Helper.GetUIOMaticTypeByAlias(typeAlias, true);
                        if (type != null)
                        {
                            var attri = type.GetCustomAttribute<UIOMaticAttribute>(true);
                            if (attri != null && !attri.ReadOnly)
                                menu.Items.Add<ActionDelete>(LocalizedTextService, true, opensDialog: true);
                        }
                    }
                    else
                    {
                        var type = Helper.GetUIOMaticTypeByAlias(id, true);
                        if (type != null)
                        {
                            var attri = type.GetCustomAttribute<UIOMaticFolderAttribute>(true);
                            if (attri != null)
                            {
                                var attri2 = attri as UIOMaticAttribute;
                                if (attri2 != null)
                                {
                                    if (!attri2.ReadOnly)
                                    {
                                        menu.Items.Add(new CreateChildEntity(LocalizedTextService));
                                    }

                                    if (attri2.RenderType == UIOMaticRenderType.Tree)
                                        menu.Items.Add(new RefreshNode(refreshText, true));
                                }
                                else
                                {
                                    menu.Items.Add(new RefreshNode(refreshText, true));
                                }
                            }
                        }
                    }


                }
                return menu;
            }
        }

        IEnumerable<SearchResultEntity> ISearchableTree.Search(string query, int pageSize, long pageIndex, out long totalFound, string searchFrom)
        {
            totalFound = 0;
            return new List<SearchResultEntity>();
        }
    }


}
