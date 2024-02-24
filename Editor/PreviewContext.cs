using System;
using System.Collections.Generic;
using System.Linq;
using net.rs64.TexTransTool.Build;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace net.rs64.TexTransTool
{
    internal class PreviewContext : ScriptableSingleton<PreviewContext>
    {
        [SerializeField]
        private Object previweing = null;

        protected PreviewContext()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ExitPreview;
            AssemblyReloadEvents.beforeAssemblyReload += ExitPreview;
            EditorSceneManager.sceneClosing -= ExitPreview;
            EditorSceneManager.sceneClosing += ExitPreview;
            DestroyCall.OnDestroy -= DestroyObserve;
            DestroyCall.OnDestroy += DestroyObserve;
        }

        private void OnEnable()
        {
            EditorApplication.update += () =>
            {
                if (!AnimationMode.InAnimationMode()) previweing = null;
            };
        }

        public static bool IsPreviewing(TexTransBehavior transformer) => transformer == instance.previweing;
        public static bool IsPreviewContains => instance.previweing != null;

        private void DrawApplyAndRevert<T>(T target, Action<T> apply)
            where T : Object
        {
            if (target == null) return;
            if (previweing == null && AnimationMode.InAnimationMode())
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button("Common:PreviewNotAvailable".Glc());
                EditorGUI.EndDisabledGroup();
            }
            else if (previweing == null)
            {
                if (GUILayout.Button("Common:Preview".Glc()))
                {
                    StartPreview(target, apply);
                }
            }
            else if (previweing == target)
            {
                if (GUILayout.Button("Common:ExitPreview".Glc()))
                {
                    ExitPreview();
                }
            }
            else
            {
                if (GUILayout.Button("Common:OverridePreviewThis".Glc()))
                {
                    ExitPreview();
                    StartPreview(target, apply);
                }
            }
        }
        public void DrawApplyAndRevert(TexTransBehavior target)
        {
            DrawApplyAndRevert(target, TexTransBehaviorApply);
        }

        void StartPreview<T>(T target, Action<T> applyAction) where T : Object
        {
            previweing = target;
            AnimationMode.StartAnimationMode();
            try
            {
                applyAction(target);
            }
            catch
            {
                AnimationMode.StopAnimationMode();
                EditorUtility.ClearProgressBar();
                previweing = null;
                throw;
            }
        }
        static void TexTransBehaviorApply(TexTransBehavior targetTTBehavior)
        {
            AnimationMode.BeginSampling();
            try
            {
                RenderersDomain previewDomain = null;
                var marker = DomainMarkerFinder.FindMarker(targetTTBehavior.gameObject);
                if (marker != null) { previewDomain = new AvatarDomain(marker, true, false, true); }
                else { previewDomain = new RenderersDomain(targetTTBehavior.GetRenderers, true, false, true); }

                switch (targetTTBehavior)
                {
                    case PhaseDefinition phaseDefinition:
                        {
                            phaseDefinition.Apply(previewDomain);
                            break;
                        }
                    case TexTransGroup texTransGroup:
                        {
                            static IEnumerable<TexTransBehavior> FinedTTGroupBehaviors(TexTransGroup texTransGroup) { return texTransGroup.Targets.Where(i => i is not PhaseDefinition).SelectMany(i => i is TexTransGroup ttg ? FinedTTGroupBehaviors(ttg) : new[] { i }); }

                            var phaseOnTf = AvatarBuildUtils.FindAtPhase(texTransGroup.gameObject);
                            AvatarBuildUtils.WhiteList(phaseOnTf, new(FinedTTGroupBehaviors(texTransGroup)));
                            ExecutePhases(previewDomain, phaseOnTf);
                            break;
                        }
                    case PreviewGroup previewGroup:
                        {
                            var phaseOnTf = AvatarBuildUtils.FindAtPhase(previewGroup.gameObject);
                            ExecutePhases(previewDomain, phaseOnTf);
                            break;
                        }
                    default:
                        {
                            targetTTBehavior.Apply(previewDomain);
                            break;
                        }
                }

                previewDomain.EditFinish();
            }
            finally
            {
                AnimationMode.EndSampling();
            }

            static void ExecutePhases(RenderersDomain previewDomain, Dictionary<TexTransPhase, List<TexTransBehavior>> phaseOnTf)
            {
                foreach (var tf in TexTransGroup.TextureTransformerFilter(phaseOnTf[TexTransPhase.BeforeUVModification])) { tf.Apply(previewDomain); }
                previewDomain.MergeStack();
                foreach (var tf in TexTransGroup.TextureTransformerFilter(phaseOnTf[TexTransPhase.UVModification])) { tf.Apply(previewDomain); }
                foreach (var tf in TexTransGroup.TextureTransformerFilter(phaseOnTf[TexTransPhase.AfterUVModification])) { tf.Apply(previewDomain); }
                foreach (var tf in TexTransGroup.TextureTransformerFilter(phaseOnTf[TexTransPhase.UnDefined])) { tf.Apply(previewDomain); }
            }
        }

        public void ExitPreview()
        {
            if (previweing == null) { return; }
            previweing = null;
            AnimationMode.StopAnimationMode();
        }
        public void ExitPreview(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            ExitPreview();
        }
        public void DestroyObserve(TexTransBehavior texTransBehavior)
        {
            if (IsPreviewing(texTransBehavior)) { instance.ExitPreview(); }
        }

        internal void RePreview()
        {
            if (!IsPreviewContains) { return; }
            if (previweing is not TexTransBehavior texTransBehavior) { return; }
            var target = texTransBehavior;
            ExitPreview();
            StartPreview(target, TexTransBehaviorApply);
        }
    }
}
