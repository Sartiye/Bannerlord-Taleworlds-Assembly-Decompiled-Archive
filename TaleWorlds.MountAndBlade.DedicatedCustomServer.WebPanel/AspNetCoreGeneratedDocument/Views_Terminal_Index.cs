using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;

namespace AspNetCoreGeneratedDocument;

[RazorCompiledItemMetadata("Identifier", "/Views/Terminal/Index.cshtml")]
[CreateNewOnMetadataUpdate]
internal sealed class Views_Terminal_Index : RazorPage<dynamic>
{
	[RazorInject]
	public IModelExpressionProvider ModelExpressionProvider { get; private set; }

	[RazorInject]
	public IUrlHelper Url { get; private set; }

	[RazorInject]
	public IViewComponentHelper Component { get; private set; }

	[RazorInject]
	public IJsonHelper Json { get; private set; }

	[RazorInject]
	public IHtmlHelper<dynamic> Html { get; private set; }

	public override async Task ExecuteAsync()
	{
		WriteLiteral("\r\n");
		base.ViewData["Title"] = "Console";
		WriteLiteral("\r\n<h2>Terminal</h2>\r\n\r\n<div id=\"react-app\">Loading...</div>\r\n\r\n");
		DefineSection("scripts", (RenderAsyncDelegate)async delegate
		{
			WriteLiteral("\r\n    <script type=\"text/javascript\">\r\n        EntryPoint.renderConsole();\r\n    </script>\r\n");
		});
	}
}
