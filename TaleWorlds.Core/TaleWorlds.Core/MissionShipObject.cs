using System;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.Core;

public class MissionShipObject : MBObjectBase
{
	private MBList<ShipSail> _sails = new MBList<ShipSail>();

	public string Prefab { get; private set; }

	public Vec2 DeploymentArea { get; private set; }

	public float Mass { get; private set; }

	public float FloatingForceMultiplier { get; private set; }

	public float MaximumSubmergedVolumeRatio { get; private set; }

	public Vec3 RudderStockPosition { get; private set; }

	public float MaxLateralDragShift { get; private set; }

	public float LateralDragShiftCriticalAngle { get; private set; }

	public ShipPhysicsReference PhysicsReference { get; private set; }

	public Vec3 MomentOfInertiaMultiplier { get; private set; }

	public LinearFrictionTerm LinearFrictionMultiplier { get; private set; }

	public Vec3 AngularFrictionMultiplier { get; private set; }

	public float TorqueMultiplierOfLateralBuoyantForces { get; private set; }

	public Vec3 TorqueMultiplierOfVerticalBuoyantForces { get; private set; }

	public float OarsmenForceMultiplier { get; private set; }

	public float OarsTipSpeed { get; private set; }

	public float OarFrictionMultiplier { get; private set; }

	public MBReadOnlyList<ShipSail> Sails => _sails;

	public int OarCount { get; private set; }

	public float RudderBladeLength { get; private set; }

	public float RudderBladeHeight { get; private set; }

	public float RudderDeflectionCoef { get; private set; }

	public float RudderRotationMax { get; private set; }

	public float RudderRotationRate { get; private set; }

	public float RudderForceMax { get; private set; }

	public float MaxLinearSpeed { get; private set; }

	public float MaxLinearAccel { get; private set; }

	public float MaxAngularSpeed { get; private set; }

	public float MaxAngularAccel { get; private set; }

	public float PartialHitPointsRatio { get; private set; }

	public bool HasSails => _sails.Count > 0;

	public bool HasValidRudderStockPosition => RudderStockPosition.IsValid;

	public string ShipPhysicsReferenceId { get; private set; }

	public float BowAngleLimitFromCenterline { get; private set; }

	public MissionShipObject()
	{
	}

	public MissionShipObject(string stringId)
		: base(stringId)
	{
	}

	public void SetPhysicsReference(ShipPhysicsReference physicsReference)
	{
		PhysicsReference = physicsReference;
	}

	public override void Deserialize(MBObjectManager objectManager, XmlNode node)
	{
		base.Deserialize(objectManager, node);
		Prefab = DeserializeScalarAttribute<string>(node, "prefab", isRequiredAttribute: true, out var isAttributeValid);
		ShipPhysicsReferenceId = DeserializeScalarAttribute<string>(node, "ship_physics_reference_id", isRequiredAttribute: true, out var _);
		Mass = DeserializeScalarAttribute<float>(node, "mass", isRequiredAttribute: true, out isAttributeValid);
		FloatingForceMultiplier = DeserializeFloatAttribute(node, "floating_force_multiplier", isRequiredAttribute: false, out isAttributeValid, 0.01f, 5f, 1f);
		MaximumSubmergedVolumeRatio = DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", isRequiredAttribute: false, out isAttributeValid, 0.01f, 5f, 0.7f);
		OarsTipSpeed = DeserializeFloatAttribute(node, "oars_tip_speed", isRequiredAttribute: true, out isAttributeValid, float.MinValue, float.MaxValue, 1f);
		OarsmenForceMultiplier = DeserializeFloatAttribute(node, "oarsmen_force_multiplier", isRequiredAttribute: false, out isAttributeValid, float.MinValue, float.MaxValue, 1f);
		OarFrictionMultiplier = DeserializeFloatAttribute(node, "oar_friction_multiplier", isRequiredAttribute: false, out isAttributeValid, float.MinValue, float.MaxValue, 1f);
		OarCount = DeserializeScalarAttribute<int>(node, "oar_count", isRequiredAttribute: true, out isAttributeValid);
		RudderBladeLength = DeserializeScalarAttribute<float>(node, "rudder_blade_length", isRequiredAttribute: true, out isAttributeValid);
		RudderBladeHeight = DeserializeScalarAttribute<float>(node, "rudder_blade_height", isRequiredAttribute: true, out isAttributeValid);
		RudderDeflectionCoef = DeserializeScalarAttribute<float>(node, "rudder_deflection_coef", isRequiredAttribute: true, out isAttributeValid);
		RudderRotationMax = DeserializeScalarAttribute<float>(node, "rudder_rotation_max", isRequiredAttribute: true, out isAttributeValid) * (System.MathF.PI / 180f);
		RudderRotationRate = DeserializeScalarAttribute<float>(node, "rudder_rotation_rate", isRequiredAttribute: true, out isAttributeValid) * (System.MathF.PI / 180f);
		RudderForceMax = DeserializeScalarAttribute<float>(node, "rudder_force_max", isRequiredAttribute: true, out isAttributeValid);
		MaxLinearSpeed = DeserializeFloatAttribute(node, "max_linear_speed", isRequiredAttribute: true, out isAttributeValid, 1f, 100f, 0f, "m/s");
		MaxLinearAccel = DeserializeFloatAttribute(node, "max_linear_acceleration", isRequiredAttribute: true, out isAttributeValid, 0.1f, 50f, 0f, "m/s^2");
		MaxAngularSpeed = DeserializeFloatAttribute(node, "max_angular_speed", isRequiredAttribute: true, out isAttributeValid, 1f, 180f, 0f, "deg") * (System.MathF.PI / 180f);
		MaxAngularAccel = DeserializeFloatAttribute(node, "max_angular_acceleration", isRequiredAttribute: true, out isAttributeValid, 1f, 180f, 0f, "deg/s") * (System.MathF.PI / 180f);
		BowAngleLimitFromCenterline = DeserializeFloatAttribute(node, "bow_angle_limit_from_centerline", isRequiredAttribute: true, out isAttributeValid, 0f, 90f);
		DeploymentArea = Deserialize2DDimensionElement(node, "deployment_area", isRequiredElement: true, out var isElementValid);
		_sails = DeserializeSailsElement(node, out isElementValid);
		MomentOfInertiaMultiplier = DeserializeVectorElement(node, "moment_of_inertia_multiplier", isRequiredElement: false, out var isElementValid2);
		if (!isElementValid2)
		{
			MomentOfInertiaMultiplier = new Vec3(1f, 1f, 1f);
		}
		LinearFrictionMultiplier = DeserializeLinearDragTermElement(node, "linear_friction_multiplier", out var isElementValid3);
		if (!isElementValid3)
		{
			LinearFrictionMultiplier = LinearFrictionTerm.One;
		}
		else if (!LinearFrictionMultiplier.IsValid)
		{
			Debug.FailedAssert("LinearFrictionMultiplier.IsValid", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "Deserialize", 183);
			LinearFrictionMultiplier = LinearFrictionTerm.One;
		}
		AngularFrictionMultiplier = DeserializeAngularDragTermElement(node, "angular_friction_multiplier", out var isElementValid4);
		if (!isElementValid4)
		{
			AngularFrictionMultiplier = Vec3.One;
		}
		else if (AngularFrictionMultiplier.x <= 0f || AngularFrictionMultiplier.y <= 0f || AngularFrictionMultiplier.z <= 0f)
		{
			Debug.FailedAssert("(AngularFrictionMultiplier.x > 0) && (AngularFrictionMultiplier.y > 0) && (AngularFrictionMultiplier.z > 0)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "Deserialize", 197);
			AngularFrictionMultiplier = Vec3.One;
		}
		MaxLateralDragShift = DeserializeFloatAttribute(node, "lateral_drag_shift_max", isRequiredAttribute: false, out isAttributeValid, 0f, 100f, 0f, "m");
		LateralDragShiftCriticalAngle = DeserializeFloatAttribute(node, "lateral_drag_shift_critical_angle", isRequiredAttribute: false, out isAttributeValid, 0f, 90f, 0f, "deg") * (System.MathF.PI / 180f);
		Vec3 rudderStockPosition = DeserializeVectorElement(node, "rudder_stock_position", isRequiredElement: false, out isElementValid);
		if (isElementValid)
		{
			RudderStockPosition = rudderStockPosition;
		}
		else
		{
			RudderStockPosition = Vec3.Invalid;
		}
		MaximumSubmergedVolumeRatio = DeserializeFloatAttribute(node, "maximum_submerged_volume_ratio", isRequiredAttribute: false, out isAttributeValid, float.MinValue, float.MaxValue, 0.7f);
		PartialHitPointsRatio = DeserializeFloatAttribute(node, "partial_hit_points_ratio", isRequiredAttribute: true, out isAttributeValid);
		TorqueMultiplierOfLateralBuoyantForces = DeserializeFloatAttribute(node, "torque_multiplier_of_lateral_buoyant_forces", isRequiredAttribute: false, out isAttributeValid, float.MinValue, float.MaxValue, 0.5f);
		TorqueMultiplierOfVerticalBuoyantForces = DeserializeVectorElement(node, "torque_multiplier_of_vertical_buoyant_forces", isRequiredElement: false, out var isElementValid5);
		if (!isElementValid5)
		{
			TorqueMultiplierOfVerticalBuoyantForces = new Vec3(1f, 1f, 1f);
		}
		if (objectManager != null)
		{
			ShipPhysicsReference @object = objectManager.GetObject<ShipPhysicsReference>(ShipPhysicsReferenceId);
			PhysicsReference = @object;
		}
		else
		{
			PhysicsReference = ShipPhysicsReference.Default;
		}
	}

	private T DeserializeScalarAttribute<T>(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
	{
		XmlAttribute xmlAttribute = node.Attributes[attributeName];
		isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText);
		if (isAttributeValid)
		{
			return (T)Convert.ChangeType(xmlAttribute.Value, typeof(T));
		}
		AssertFieldValidity(!isRequiredAttribute, attributeName);
		return default(T);
	}

	private float DeserializeFloatAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid, float minValue = float.MinValue, float maxValue = float.MaxValue, float defaultValue = 0f, string unitString = "")
	{
		float num = DeserializeScalarAttribute<float>(node, attributeName, isRequiredAttribute, out isAttributeValid);
		if (isAttributeValid)
		{
			if (num < minValue)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + attributeName + " field is less than the required minimum value of " + minValue + " " + unitString + ".", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 267);
				num = minValue;
			}
			if (num > maxValue)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + attributeName + " field is greater than the required maximum value of " + maxValue + " " + unitString + ".", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeFloatAttribute", 273);
				num = maxValue;
			}
		}
		else
		{
			num = defaultValue;
		}
		return num;
	}

	private Vec2 Deserialize2DDimensionElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
	{
		Vec2 zero = Vec2.Zero;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			foreach (XmlAttribute attribute in xmlNode.Attributes)
			{
				string text = attribute.Name.ToLower();
				float num = float.Parse(attribute.Value);
				switch (text)
				{
				case "width":
					zero.x = num;
					break;
				case "length":
				case "height":
					zero.y = num;
					break;
				}
			}
		}
		else
		{
			AssertFieldValidity(!isRequiredElement, elementName);
		}
		return zero;
	}

	private Vec3 Deserialize3DDimensionElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
	{
		Vec3 zero = Vec3.Zero;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			foreach (XmlAttribute attribute in xmlNode.Attributes)
			{
				string text = attribute.Name.ToLower();
				float num = float.Parse(attribute.Value);
				switch (text)
				{
				case "width":
					zero.x = num;
					break;
				case "length":
					zero.y = num;
					break;
				case "height":
					zero.z = num;
					break;
				}
			}
		}
		else
		{
			AssertFieldValidity(!isRequiredElement, elementName);
		}
		return zero;
	}

	private Vec3 DeserializeVectorElement(XmlNode node, string elementName, bool isRequiredElement, out bool isElementValid)
	{
		Vec3 invalid = Vec3.Invalid;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			foreach (XmlAttribute attribute in xmlNode.Attributes)
			{
				string text = attribute.Name.ToLower();
				float num = float.Parse(attribute.Value);
				switch (text)
				{
				case "x":
					invalid.x = num;
					break;
				case "y":
					invalid.y = num;
					break;
				case "z":
					invalid.z = num;
					break;
				}
			}
		}
		else
		{
			AssertFieldValidity(!isRequiredElement, elementName);
		}
		return invalid;
	}

	private LinearFrictionTerm DeserializeLinearDragTermElement(XmlNode node, string elementName, out bool isElementValid)
	{
		float num = 0f;
		float left = 0f;
		float forward = 0f;
		float backward = 0f;
		float up = 0f;
		float down = 0f;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			XmlAttributeCollection attributes = xmlNode.Attributes;
			if (attributes.Count != 5)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + elementName + " element must have exactly " + 5 + " attributes for directional drag multipliers", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeLinearDragTermElement", 416);
				isElementValid = false;
			}
			else
			{
				foreach (XmlAttribute item in attributes)
				{
					string text = item.Name.ToLower();
					string value = item.Value;
					switch (text)
					{
					case "side":
						num = float.Parse(value);
						left = num;
						break;
					case "forward":
						forward = float.Parse(value);
						break;
					case "backward":
						backward = float.Parse(value);
						break;
					case "up":
						up = float.Parse(value);
						break;
					case "down":
						down = float.Parse(value);
						break;
					}
				}
			}
		}
		LinearFrictionTerm result = new LinearFrictionTerm(num, left, forward, backward, up, down);
		isElementValid = isElementValid && result.IsValid;
		return result;
	}

	private Vec3 DeserializeAngularDragTermElement(XmlNode node, string elementName, out bool isElementValid)
	{
		Vec3 one = Vec3.One;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		XmlNode xmlNode = node.SelectSingleNode(elementName);
		isElementValid = xmlNode != null;
		if (isElementValid)
		{
			XmlAttributeCollection attributes = xmlNode.Attributes;
			if (attributes.Count != 3)
			{
				Debug.FailedAssert("ShipObject(" + base.StringId + "): " + elementName + " element must have exactly " + 3 + " attributes for angular drag friction multipliers", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.Core\\MissionShipObject.cs", "DeserializeAngularDragTermElement", 482);
				isElementValid = false;
			}
			else
			{
				foreach (XmlAttribute item in attributes)
				{
					string text = item.Name.ToLower();
					string value = item.Value;
					switch (text)
					{
					case "pitch":
						one.x = float.Parse(value);
						flag = true;
						break;
					case "roll":
						one.y = float.Parse(value);
						flag2 = true;
						break;
					case "yaw":
						one.z = float.Parse(value);
						flag3 = true;
						break;
					}
				}
			}
		}
		isElementValid = isElementValid && flag && flag2 && flag3;
		return one;
	}

	private MBList<ShipSail> DeserializeSailsElement(XmlNode node, out bool isElementValid)
	{
		MBList<ShipSail> mBList = new MBList<ShipSail>();
		XmlNode xmlNode = node.SelectSingleNode("sails");
		isElementValid = xmlNode != null && xmlNode.ChildNodes.Count > 0;
		if (isElementValid)
		{
			for (int i = 0; i < xmlNode.ChildNodes.Count; i++)
			{
				XmlNode node2 = xmlNode.ChildNodes[i];
				bool isAttributeValid;
				int index = DeserializeScalarAttribute<int>(node2, "index", isRequiredAttribute: true, out isAttributeValid);
				SailType type = DeserializeSailTypeAttribute(node2, "type", isRequiredAttribute: true, out isAttributeValid);
				float forceMultiplier = DeserializeFloatAttribute(node2, "force_multiplier", isRequiredAttribute: false, out isAttributeValid, float.MinValue, float.MaxValue, 1f);
				float leftRotationLimit = DeserializeFloatAttribute(node2, "left_rotation_limit", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, 0f, "deg") * (System.MathF.PI / 180f);
				float rightRotationLimit = DeserializeFloatAttribute(node2, "right_rotation_limit", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, 0f, "deg") * (System.MathF.PI / 180f);
				float rotationRate = DeserializeFloatAttribute(node2, "rotation_rate", isRequiredAttribute: true, out isAttributeValid, 0f, float.MaxValue, 0f, "deg/s") * (System.MathF.PI / 180f);
				ShipSail item = new ShipSail(this, index, type, forceMultiplier, leftRotationLimit, rightRotationLimit, rotationRate);
				mBList.Add(item);
			}
		}
		return mBList;
	}

	private SailType DeserializeSailTypeAttribute(XmlNode node, string attributeName, bool isRequiredAttribute, out bool isAttributeValid)
	{
		XmlAttribute xmlAttribute = node.Attributes[attributeName];
		SailType result = SailType.Square;
		isAttributeValid = xmlAttribute != null && !string.IsNullOrEmpty(xmlAttribute.InnerText) && Enum.TryParse<SailType>(xmlAttribute.Value, ignoreCase: true, out result);
		if (!isAttributeValid)
		{
			AssertFieldValidity(!isRequiredAttribute, attributeName);
		}
		return result;
	}

	private void AssertFieldValidity(bool assert, string fieldName)
	{
	}
}
