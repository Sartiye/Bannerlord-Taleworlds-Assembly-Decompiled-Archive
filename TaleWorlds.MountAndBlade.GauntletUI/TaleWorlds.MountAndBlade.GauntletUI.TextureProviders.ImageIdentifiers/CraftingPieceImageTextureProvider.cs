using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.ImageIdentifiers;

public class CraftingPieceImageTextureProvider : ImageIdentifierTextureProvider
{
	protected override void OnCreateImageWithId(string id, string additionalArgs)
	{
		if (string.IsNullOrEmpty(id))
		{
			OnTextureCreated(null);
			return;
		}
		CraftingPiece @object = MBObjectManager.Instance.GetObject<CraftingPiece>(id.Split(new char[1] { '$' })[0]);
		if (@object == null)
		{
			Debug.FailedAssert("WRONG CraftingPiece IMAGE IDENTIFIER ID", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\TextureProviders\\ImageIdentifiers\\CraftingPieceImageTextureProvider.cs", "OnCreateImageWithId", 22);
			OnTextureCreated(null);
		}
		else
		{
			base.ThumbnailCreationData = new CraftingPieceCreationData(@object, id.Split(new char[1] { '$' })[1], base.OnTextureCreated, base.OnTextureCreationCancelled);
			ThumbnailCacheManager.Current.CreateTexture(base.ThumbnailCreationData);
		}
	}
}
