using System;
using UnityEngine;

namespace ROLib
{
    public class ROModuleSeeThroughObject : PartModule
    {
        [KSPField]
        public string transformName = "";

        [KSPField]
        public float screenRadius = 1f;

        [KSPField]
        public float proximityBias = 1.4f;

        [KSPField]
        public float minOpacity = 0.4f;

        [KSPField]
        public int leadModuleIndex = -1;

        [KSPField]
        public float leadModuleTgtValue = 1f;

        [KSPField]
        public float leadModuleTgtGain = 2f;

        [NonSerialized]
        private MeshRenderer[] mrs;

        [NonSerialized]
        private Transform trf;

        private bool setup;

        private float screenHeightRecip = 1f;

        [NonSerialized]
        private IScalarModule leadModule;

        private MaterialPropertyBlock mpb;

        public override void OnStart(StartState state)
        {
            Debug.Log($"CLAYELADDEDLOGS ROModuleSeeThroughObject called in state {state}.");
            
            setup = false;
            //if (!HighLogic.LoadedSceneIsEditor)
            //{
            //    return;
            //}
            trf = base.part.FindModelTransform(transformName);
            if (trf == null)
            {
                ROLLog.error("No Transform exists in part " + base.part.partInfo.name + " called " + transformName);
                return;
            }
            mrs = trf.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            if (mrs == null)
            {
                ROLLog.error("No MeshRenderer components found in " + transformName + " on part " + base.part.partInfo.name);
                return;
            }
            if (leadModuleIndex != -1)
            {
                leadModule = base.part.Modules[leadModuleIndex] as IScalarModule;
                if (leadModule == null)
                {
                    ROLLog.error("Module at index " + leadModuleIndex + " is not an IScalarModule on part " + base.part.partInfo.name);
                    return;
                }
            }

            if (SetShaders())
            {
                if (base.part.variants != null)
                {
                    GameEvents.onVariantApplied.Add(OnVariantApplied);
                }

                screenHeightRecip = 1f / (float)Screen.height;
                setup = true;
            }
        }

        private void OnVariantApplied(Part part, PartVariant variant)
        {
            if (part == null || part != base.part)
            {
                return;
            }

            SetShaders();
        }

        private void LateUpdate()
        {
            if (EditorLogic.fetch != null && setup && EditorLogic.fetch.ship.Contains(base.part))
            {
                MouseFadeUpdate();
            }
        }

        private void OnDestroy()
        {
            if (base.part.variants != null)
            {
                GameEvents.onVariantApplied.Remove(OnVariantApplied);
            }
        }

        private void MouseFadeUpdate()
        {
            float cursorProximity = GetCursorProximity(Input.mousePosition, screenRadius, trf, Camera.main);
            cursorProximity = Mathf.Pow(Mathf.Clamp01(cursorProximity), proximityBias);
            float opacity = Mathf.Max(1f - cursorProximity, minOpacity);
            SetOpacity(leadModule, opacity);
        }

        private float GetCursorProximity(Vector3 cursorPosition, float range, Transform trf, Camera referenceCamera)
        {
            float num = Mathf.Tan(referenceCamera.fieldOfView * 0.5f * ((float)Math.PI / 180f)) * (base.part.partTransform.transform.position - referenceCamera.transform.position).sqrMagnitude;
            float num2 = range * range / num;
            cursorPosition *= screenHeightRecip;
            Vector3 vector = referenceCamera.WorldToScreenPoint(trf.position) * screenHeightRecip;
            Vector3 vector2 = cursorPosition - vector;
            float sqrMagnitude = Vector3.ProjectOnPlane(vector2, Vector3.forward).sqrMagnitude;
            return Mathf.Clamp01(1f - sqrMagnitude / num2);
        }

        private bool SetShaders()
        {
            //Material normalTexture = base.part.variants.SelectedVariant.Materials.FirstOrDefault(m => m.HasProperty("_BumpMap"));
            //if (normalTexture == null)
            //{
            //    ROLLog.error("[ModuleSeeThroughObject]: No material with a normal map found in the selected variant of part " + base.part.partInfo.name);
            //    return;
            //}

            //material = new Material

            Shader shader;

            if (HighLogic.LoadedSceneIsFlight)
            {
                shader = Shader.Find("KSP/Bumped Specular Opaque (Cutoff)");
                if (shader == null)
                {
                    ROLLog.error($"No shader called KSP/Bumped Specular Opaque (Cutoff) found for part {base.part.partInfo.name}.");
                    return false;
                }
            }
            else
            {
                shader = Shader.Find("KSP/Bumped Specular (Cutoff)");
                if (shader == null)
                {
                    ROLLog.error($"No shader called KSP/Bumped Specular (Cutoff) found for part {base.part.partInfo.name}.");
                    return false;
                }
            }

            int num = mrs.Length;
            Debug.Log($"CLAYELADDEDLOGS SetShaders num: {num}");
            while (num-- > 0)
            {
                mrs[num].material.shader = shader;
            }
            float opacity = 1f;
            SetOpacity(leadModule, opacity);

            return true;
        }

        private void SetOpacity(IScalarModule leadModule, float o)
        {
            if (leadModule != null)
            {
                SetOpacity(Mathf.Clamp01(o + Mathf.Abs(leadModuleTgtValue - leadModule.GetScalar) * leadModuleTgtGain));
            }
            else
            {
                SetOpacity(o);
            }
        }

        public void SetOpacity(float o)
        {
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
            mpb.SetFloat(PropertyIDs._Opacity, o);

            int num = mrs.Length;
            Debug.Log($"CLAYELADDEDLOGS SetOpacity opacity: {o}, num: {num}");
            while (num-- > 0)
            {
                //mrs[num].SetPropertyBlock(mpb);
                float originalOpacity = mrs[num].material.GetFloat(PropertyIDs._Opacity);
                mrs[num].material.SetFloat(PropertyIDs._Opacity, o);
                Debug.Log($"CLAYELADDEDLOGS SetOpacity mrs[num].material.mainTexture.name: {mrs[num].material.mainTexture.name}, originalOpacity: {originalOpacity}");
            }

            //if (attachedFlagParts == null)
            //{
            //    return;
            //}
            //for (int i = 0; i < attachedFlagParts.Count; i++)
            //{
            //    for (int j = 0; j < attachedFlagParts[i].flagMeshRenderers.Count; j++)
            //    {
            //        attachedFlagParts[i].flagMeshRenderers[j].SetPropertyBlock(mpb);
            //    }
            //}
        }
    }
}