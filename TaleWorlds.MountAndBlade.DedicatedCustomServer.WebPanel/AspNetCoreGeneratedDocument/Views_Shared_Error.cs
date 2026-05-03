using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;
using TaleWorlds.MountAndBlade.Diamond.Dashboard.Models;

namespace AspNetCoreGeneratedDocument;

[RazorCompiledItemMetadata("Identifier", "/Views/Shared/Error.cshtml")]
[CreateNewOnMetadataUpdate]
internal sealed class Views_Shared_Error : RazorPage<ErrorViewModel>
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
	public IHtmlHelper<ErrorViewModel> Html { get; private set; }

	public override async Task ExecuteAsync()
	{
		base.ViewData["Title"] = "Error";
		WriteLiteral("\r\n<h1 class=\"text-danger\">Error.</h1>\r\n<h2 class=\"text-danger\">An error occurred while processing your request.</h2>\r\n\r\n");
		if (base.Model.ShowRequestId)
		{
			WriteLiteral("    <p>\r\n        <strong>Request ID:</strong> <code>");
			Write(base.Model.RequestId);
			WriteLiteral("</code>\r\n    </p>\r\n");
		}
		WriteLiteral("\r\n<h3>Development Mode</h3>\r\n<p>\r\n    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.\r\n</p>\r\n<p>\r\n    <strong>Development environment should not be enabled in deployed applications</strong>, as it can result in sensitive information from exceptions being displayed to end users. For local debugging, development environment can be enabled by setting the <strong>ASPNETCORE_ENVIRONMENT</strong> environment variable to <strong>Development</strong>, and restarting the application.\r\n</p>\r\n");
	}
}
