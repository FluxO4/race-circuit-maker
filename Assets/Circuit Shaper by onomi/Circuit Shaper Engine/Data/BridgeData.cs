using System.Collections.Generic;
using System.Numerics;

namespace OnomiCircuitShaper.Engine.Data
{
    /// <summary>
    /// Contains all the parameters needed to construct a bridge mesh.
    /// A bridge can either be generated from a simple, procedural template
    /// or from a custom list of 2D shape points.
    /// </summary>
    [System.Serializable]
    public class BridgeData
    {
        //enabled flag
        public bool Enabled = true;



        /// <summary>
        /// The material index to use when rendering the bridge mesh.
        /// </summary>
        public int MaterialIndex = 0;

        /// <summary>
        /// If true, the bridge shape will be generated using the procedural template parameters below.
        /// If false, it will be generated from the custom 'BridgeShapePoints' list.
        /// </summary>
        public bool UseTemplate = true;

        /// <summary>
        /// A list of 2D points that define one half of the bridge's cross-section profile.
        /// This is used when 'UseTemplate' is false to create a custom bridge shape.
        /// The shape will be mirrored to create the full bridge.
        /// </summary>
        public List<SerializableVector2> BridgeShapePoints = new List<SerializableVector2>();

        /// <summary>
        /// Controls the tiling of the UV coordinates on the bridge mesh.
        /// </summary>
        public SerializableVector2 UVTile = (SerializableVector2)Vector2.One;

        /// <summary>
        /// Controls the offset of the UV coordinates on the bridge mesh.
        /// </summary>
        public SerializableVector2 UVOffset = (SerializableVector2)Vector2.Zero;

        #region Template Properties
        /// <summary>
        /// (Template) The width of the top edge or curb of the bridge.
        /// </summary>
        public float TemplateEdgeWidth = 0.5f;

        /// <summary>
        /// (Template) The total height of the bridge structure, from the top surface to the bottom.
        /// </summary>
        public float TemplateBridgeHeight = 2.0f;

        /// <summary>
        /// (Template) The width of the support flange that extends out from under the bridge deck.
        /// </summary>
        public float TemplateFlangeWidth = 0.5f;

        /// <summary>
        /// (Template) The thickness of the support flange.
        /// </summary>
        public float TemplateFlangeHeight = 0.5f;

        /// <summary>
        /// (Template) The vertical distance from the road surface to the top of the flange.
        /// </summary>
        public float TemplateFlangeDepth = 0.2f;

        /// <summary>
        /// (Template) The height of the curb at the edge of the road surface.
        /// </summary>
        public float TemplateCurbHeight = 0.3f;
        #endregion
    }
}
