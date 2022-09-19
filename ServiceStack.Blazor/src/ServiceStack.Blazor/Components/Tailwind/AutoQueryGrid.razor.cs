﻿using Microsoft.AspNetCore.Components;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Blazor;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;

namespace ServiceStack.Blazor.Components.Tailwind;

public class AutoQueryGridBase<Model> : AuthBlazorComponentBase
{
    public DataGridBase<Model>? DataGrid = default!;
    public string CacheKey => $"{Id}/{nameof(ApiPrefs)}/{typeof(Model).Name}";
    [Inject] public LocalStorage LocalStorage { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public IJSRuntime JS { get; set; }

    [Parameter] public string Id { get; set; } = "AutoQueryGrid";
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment Columns { get; set; }
    [CascadingParameter] public AppMetadata? AppMetadata { get; set; }
    [Parameter] public bool AllowSelection { get; set; } = true;
    [Parameter] public bool AllowFiltering { get; set; } = true;
    [Parameter] public bool AllowQueryFilters { get; set; } = true;

    public List<AutoQueryConvention> FilterDefinitions { get; set; } = BlazorConfig.Instance.DefaultFilters;
    [Parameter] public Apis? Apis { get; set; }

    /// <summary>
    /// Replace entire Toolbar
    /// </summary>
    [Parameter] public RenderFragment? Toolbar { get; set; }
    /// <summary>
    /// Add more Toolbar buttons
    /// </summary>
    [Parameter] public RenderFragment? ToolbarButtons { get; set; }

    [Parameter] public bool ShowToolbar { get; set; } = true;
    [Parameter] public bool ShowPreferences { get; set; } = true;
    [Parameter] public bool ShowPagingNav { get; set; } = true;
    [Parameter] public bool ShowPagingInfo { get; set; } = true;
    [Parameter] public bool ShowDownloadCsv { get; set; } = true;
    [Parameter] public bool ShowCopyApiUrl { get; set; } = true;
    [Parameter] public bool ShowResetPreferences { get; set; } = true;
    [Parameter] public bool ShowFiltersView { get; set; } = true;
    [Parameter] public bool ShowNewItem { get; set; } = true;
    [Parameter] public List<Model> Items { get; set; } = new();
    [Parameter] public RenderFragment? CreateForm { get; set; }
    [Parameter] public RenderFragment? EditForm { get; set; }
    [Parameter] public Predicate<string>? DisableKeyBindings { get; set; }

    [Parameter] public EventCallback<Column<Model>> HeaderSelected { get; set; }
    [Parameter] public EventCallback<Model> RowSelected { get; set; }

    public List<Model> Results => Api?.Response?.Results ?? TypeConstants<Model>.EmptyList;
    public int Total => Api?.Response?.Total ?? Results.Count;

    public string ToolbarButtonClass { get; set; } = CssUtils.Tailwind.ToolbarButtonClass;

    protected ApiResult<QueryResponse<Model>>? Api { get; set; }
    protected ApiResult<QueryResponse<Model>>? EditApi { get; set; }

    protected bool apiLoading => Api == null;
    protected string? errorSummary => Api?.Error.SummaryMessage();

    protected enum Features
    {
        Filters
    }

    protected async Task downloadCsv() 
    {
        var apiUrl = CreateApiUrl("csv");
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", apiUrl);
        await JS.OpenAsync(apiUrl);
    }

    protected bool copiedApiUrl;

    protected async Task copyApiUrl()
    {
        var apiUrl = CreateApiUrl("json");
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", apiUrl);
        copiedApiUrl = true;
        StateHasChanged();
        await Task.Delay(3000);
        copiedApiUrl = false;
        StateHasChanged();
    }

    public string CreateApiUrl(string ext = "json")
    {
        var args = CreateRequestArgs();
        var url = args.ToUrl(ServiceStack.HttpMethods.Get, UrlExtensions.ToApiUrl);
        var absoluteUrl = Client!.BaseUri.CombineWith(url.AddQueryParam("jsconfig", "edv"));
        var formatUrl = absoluteUrl.IndexOf('?') >= 0
            ? absoluteUrl.LeftPart('?') + "." + ext + "?" + absoluteUrl.RightPart('?')
            : absoluteUrl + ".json";
        return formatUrl;
    }

    protected async Task clearPrefs() 
    {
        foreach (var c in GetColumns())
        {
            await c.RemoveSettingsAsync();
        }
        ApiPrefs.Clear();
        await LocalStorage.RemoveItemAsync(CacheKey);
        await UpdateAsync();
    }

    protected Features? open { get; set; }

    public List<Column<Model>> GetColumns() => DataGrid?.GetColumns() ?? TypeConstants<Column<Model>>.EmptyList;
    public Dictionary<string, Column<Model>> ColumnsMap => DataGrid?.ColumnsMap ?? new();

    protected int filtersCount => GetColumns().Select(x => x.Settings.Filters.Count).Sum();

    public List<MetadataPropertyType> Properties => appMetadataApi.Response?.Api.Types
        .FirstOrDefault(x => x.Name == typeof(Model).Name)?.Properties ?? new();
    public List<MetadataPropertyType> ViewModelColumns => Properties.Where(x => GetColumns().Any(c => c.Name == x.Name)).ToList();

    public Column<Model>? Filter { get; set; }
    public ApiPrefs ApiPrefs { get; set; } = new();
    protected async Task saveApiPrefs(ApiPrefs prefs)
    {
        ShowQueryPrefs = false;
        ApiPrefs = prefs;
        await LocalStorage.SetItemAsync(CacheKey, ApiPrefs);
        await UpdateAsync();
    }

    ApiResult<AppMetadata> appMetadataApi = new();
    protected PluginInfo? Plugins => AppMetadata?.Plugins;

    protected AutoQueryInfo? Plugin => Plugins?.AutoQuery;

    protected bool ShowQueryPrefs;

    protected MetadataOperationType? FindOp(string name) => X.Map(name, name =>
        appMetadataApi.Response?.Api.Operations.FirstOrDefault(x => x.Request.Name == name));

    protected MetadataOperationType QueryOp => X.Map(Apis!.Query ?? Apis.QueryInto, type => FindOp(type.Name))!;
    protected MetadataOperationType? CreateOp => X.Map(Apis!.Create, type => FindOp(type.Name))!;
    protected MetadataOperationType? UpdateOp => X.Map(Apis!.Patch ?? Apis.Update, type => FindOp(type.Name))!;
    protected MetadataOperationType? DeleteOp => X.Map(Apis!.Delete, type => FindOp(type.Name))!;

    protected string? invalidAccess => QueryOp != null ? base.InvalidAccessMessage(QueryOp) : null;
    protected string? invalidCreateAccess => CreateOp != null ? base.InvalidAccessMessage(CreateOp) : null;
    protected string? invalidUpdateAccess => UpdateOp != null ? base.InvalidAccessMessage(UpdateOp) : null;

    protected bool CanCreate => CreateOp != null && CanAccess(CreateOp);
    protected bool CanUpdate => UpdateOp != null && CanAccess(UpdateOp);
    protected bool CanDelete => DeleteOp != null && CanAccess(DeleteOp);

    [Parameter, SupplyParameterFromQuery] public int Skip { get; set; } = 0;
    [Parameter, SupplyParameterFromQuery] public bool? New { get; set; }
    [Parameter, SupplyParameterFromQuery] public string? Edit { get; set; }

    public int Take => ApiPrefs.Take;

    protected bool canFirst => Skip > 0;
    protected bool canPrev => Skip > 0;
    protected bool canNext => Results.Count >= Take;
    protected bool canLast => Results.Count >= Take;

    class QueryParams
    {
        public const string Skip = "skip";
        public const string Edit = "edit";
        public const string New = "new";
    }

    protected async Task skipTo(int value)
    {
        Skip += value;
        if (Skip < 0)
            Skip = 0;

        var lastPage = Math.Floor(Total / (double) Take) * Take;
        if (Skip > lastPage)
            Skip = (int)lastPage;

        string uri = NavigationManager.Uri.SetQueryParam(QueryParams.Skip, Skip > 0 ? $"{Skip}" : null);
        log($"skipTo ({value}) {uri}");
        NavigationManager.NavigateTo(uri);
    }

    protected async Task OnRowSelected(Model? item)
    {
        if (item == null)
        {
            OnEditDone();
            return;
        }

        if (UpdateOp != null)
        {
            var id = Properties.GetId(item);
            if (id != null)
            {
                var idStr = id.ConvertTo<string>();
                string uri = NavigationManager.Uri.SetQueryParam(QueryParams.New, null).SetQueryParam(QueryParams.Edit, idStr);
                NavigationManager.NavigateTo(uri);
            }
        }
        await RowSelected.InvokeAsync(item);
    }

    protected void OnShowNewItem(MouseEventArgs e)
    {
        string uri = NavigationManager.Uri.SetQueryParam(QueryParams.Edit, null).SetQueryParam(QueryParams.New, "true");
        NavigationManager.NavigateTo(uri);
    }


    protected Model? EditModel { get; set; }

    protected void OnEditDone()
    {
        EditModel = default;
        string uri = NavigationManager.Uri.SetQueryParam(QueryParams.Edit, null);
        NavigationManager.NavigateTo(uri);
    }

    protected void OnNewDone()
    {
        EditModel = default;
        string uri = NavigationManager.Uri.SetQueryParam(QueryParams.New, null);
        NavigationManager.NavigateTo(uri);
    }

    protected async Task OnEditSave(Model model)
    {
        lastQuery = null;
        OnEditDone();
    }

    protected void OnNewSave(Model model)
    {
        lastQuery = null;
        OnNewDone();
    }

    protected bool hasPrefs => GetColumns().Any(c => c.Filters.Count > 0 || c.Settings.SortOrder != null)
        || ApiPrefs.SelectedColumns.Count > 0;

    string? lastQuery = null;

    MetadataPropertyType PrimaryKey => Properties.GetPrimaryKey()!;

    string? primaryKeyName;
    protected string PrimaryKeyName => primaryKeyName ??= PrimaryKey.Name;

    public QueryBase CreateRequestArgs() => CreateRequestArgs(out _);

    
    public QueryBase CreateRequestArgs(out string queryString)
    {
        // PK always needed
        var selectFields = ApiPrefs.SelectedColumns.Count > 0 && !ApiPrefs.SelectedColumns.Contains(PrimaryKeyName)
            ? new List<string>(ApiPrefs.SelectedColumns) { PrimaryKeyName }
            : ApiPrefs.SelectedColumns;

        var selectedColumns = ApiPrefs.SelectedColumns.Count == 0
            ? GetColumns()
            : ApiPrefs.SelectedColumns.Select(x => ColumnsMap.TryGetValue(x, out var v) == true ? v : null)
                .Where(x => x != null).Select(x => x!).ToList();

        var orderBy = string.Join(",", selectedColumns.Where(x => x.Settings.SortOrder != null)
            .Select(x => x.Settings.SortOrder == SortOrder.Descending ? $"-{x.Name}" : x.Name));

        var fields = string.Join(',', selectFields);

        var filters = new Dictionary<string, string>();
        var sb = StringBuilderCache.Allocate();
        foreach (var column in GetColumns().Where(x => x.Settings.Filters.Count > 0))
        {
            foreach (var filter in column.Filters)
            {
                var key = filter.Key.Replace("%", column.Name);
                var value = filter.Value ?? (filter.Values != null ? string.Join(",", filter.Values) : "");
                filters[key] = value;
                sb.AppendQueryParam(key, value);
            }
        }
        if (AllowQueryFilters)
        {
            var query = Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
            foreach (string key in query)
            {
                string? value = query[key];
                var isProp = Properties.Any(x => x.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
                if (value == null || !isProp) continue;

                filters[key] = value;
                sb.AppendQueryParam(key, query[key]);
            }
        }

        var strFilters = StringBuilderCache.ReturnAndFree(sb);
        strFilters.TrimEnd('&');

        queryString = $"?skip={Skip}&take={Take}&fields={fields}&orderBy={orderBy}&{strFilters}";

        var request = Apis!.QueryRequest<Model>();
        request.Skip = Skip;
        request.Take = Take;
        request.Include = "Total";

        if (filters.Count > 0)
            request.QueryParams = filters;
        if (!string.IsNullOrEmpty(fields))
            request.Fields = fields;
        if (!string.IsNullOrEmpty(orderBy))
            request.OrderBy = orderBy;

        return request;
    }

    protected async Task UpdateAsync()
    {
        var request = CreateRequestArgs(out var newQuery);
        if (lastQuery == newQuery)
            return;
        lastQuery = newQuery;

        var requestWithReturn = (IReturn<QueryResponse<Model>>)request;
        Api = await ApiAsync(requestWithReturn);

        log($"UpdateAsync: {request.GetType().Name}({newQuery}) Succeeded: {Api.Succeeded}, Results: {Api.Response?.Results?.Count ?? 0}");
        if (!Api.Succeeded)
            log("Api: " + Api.ErrorSummary ?? Api.Error?.ErrorCode);

        StateHasChanged();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        var query = Pcl.HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        Skip = query[QueryParams.Skip]?.ConvertTo<int>() ?? 0;
        Edit = query[QueryParams.Edit];
        New = query[QueryParams.New]?.ConvertTo<bool>();

        if (Edit != null || New == true)
        {
            if (EditModel == null || Properties.GetId(EditModel)?.ToString() != Edit)
            {
                var request = Apis!.QueryRequest<Model>();
                request.QueryParams = new() {
                    [PrimaryKeyName] = Edit
                };
                var requestWithReturn = (IReturn<QueryResponse<Model>>)request;
                EditApi = await ApiAsync(requestWithReturn);
                if (EditApi.Succeeded)
                {
                    var results = EditApi.Response?.Results;
                    if (results?.Count == 1)
                    {
                        EditModel = EditApi.Response!.Results[0];
                    }
                    else if (results == null || results.Count == 0)
                    {
                        EditApi.AddFieldError(PrimaryKeyName, $"{typeof(Model).Name} with {PrimaryKeyName} '{Edit}' was not found");
                    }
                    else
                    {
                        EditApi.AddFieldError(PrimaryKeyName, $"Multiple results found for {typeof(Model).Name} with {PrimaryKeyName} '{Edit}'");
                    }
                }
            }
        }
        await UpdateAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (AppMetadata == null)
        {
            appMetadataApi = await this.ApiAppMetadataAsync();
            AppMetadata = appMetadataApi.Response;
        }
        var autoQueryFilters = AppMetadata?.Plugins?.AutoQuery?.ViewerConventions;
        if (autoQueryFilters != null)
            FilterDefinitions = autoQueryFilters;

        ApiPrefs = await LocalStorage.GetItemAsync<ApiPrefs>(CacheKey) ?? new();

        await UpdateAsync();
    }

    private DotNetObjectReference<AutoQueryGridBase<Model>>? dotnetRef;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotnetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("JS.registerKeyNav", dotnetRef);
        }
    }
    public void Dispose() => dotnetRef?.Dispose();

    protected virtual void CloseDialogs()
    {
        if (Edit != null)
            OnEditDone();
        if (New != null)
            OnNewDone();

        ShowQueryPrefs = false;
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnKeyNav(string key)
    {
        if (DisableKeyBindings != null && DisableKeyBindings(key))
            return;

        if (key == KeyCodes.Escape)
        {
            CloseDialogs();
            return;
        }
        if (key == KeyCodes.ArrowLeft && canPrev)
        {
            await skipTo(-Take);
            return;
        }
        if (key == KeyCodes.ArrowRight && canNext)
        {
            await skipTo(Take);
            return;
        }
        Model? selectedItem = DataGrid != null ? DataGrid.SelectedItem : default;
        var activeIndex = selectedItem != null 
            ? Results.FindIndex(x => selectedItem.Equals(x))
            : -1;
        if (activeIndex == -1)
        {
            var firstRow = Results.FirstOrDefault();
            if (firstRow != null && DataGrid != null)
            {
                await DataGrid!.SetSelectedItem(firstRow);
            }
            return;
        }

        var nextIndex = key switch
        {
            KeyCodes.ArrowUp => activeIndex - 1,
            KeyCodes.ArrowDown => activeIndex + 1,
            KeyCodes.Home => 0,
            KeyCodes.End => Results.Count - 1,
            _ => 0
        };
        if (nextIndex < 0)
        {
            nextIndex = Results.Count - 1;
        }
        await DataGrid!.SetSelectedItem(Results[nextIndex % Results.Count]);
    }
}