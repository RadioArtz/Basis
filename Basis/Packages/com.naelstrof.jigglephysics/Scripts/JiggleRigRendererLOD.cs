using System;
using UnityEngine;

namespace JigglePhysics
{
    public class JiggleRigRendererLOD : JiggleRigSimpleLOD
    {
        public class RendererSubscription
        {
            public bool visible;
            public Action<bool> action;
            public MonoBehaviorHider.JiggleRigLODRenderComponent rendererSubscription;
        }

        public RendererSubscription[] subscriptions;
        public int subscriptionCount;
        public bool lastVisibility;

        public void ClearRenderers()
        {
            for (int i = 0; i < subscriptionCount; i++)
            {
                subscriptions[i].rendererSubscription.VisibilityChange -= subscriptions[i].action;
            }
            subscriptions = null;
            subscriptionCount = 0;
        }
        public void SetRenderers(Renderer[] renderers)
        {
            ClearRenderers();
            MonoBehaviorHider.JiggleRigLODRenderComponent jiggleRigVisibleFlag = null;
            subscriptionCount = renderers.Length;
            subscriptions = new RendererSubscription[subscriptionCount];
            for (int i = 0; i < subscriptionCount; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer) continue;
                if (!renderer.TryGetComponent(out jiggleRigVisibleFlag))
                {
                    jiggleRigVisibleFlag = renderer.gameObject.AddComponent<MonoBehaviorHider.JiggleRigLODRenderComponent>();
                }

                var index = i;
                Action<bool> action = (visible) =>
                {
                    // Check if the index is out of bounds
                    if (index < 0 || index >= subscriptionCount)
                    {
                        Debug.LogError("Index out of bounds: " + index + ". Valid range is 0 to " + (subscriptionCount - 1));
                        return;
                    }
                    // Update the visibility at the specified index
                    subscriptions[index].visible = visible;
                    // Re-evaluate visibility
                    RevalulateVisibility();
                };
                subscriptions[i] = new RendererSubscription()
                {
                    visible = renderer.isVisible,
                    action = action,
                    rendererSubscription = jiggleRigVisibleFlag
                };
                jiggleRigVisibleFlag.VisibilityChange += action;
            }
            RevalulateVisibility();
        }

        protected override void Awake()
        {
            base.Awake();
        }
        private void RevalulateVisibility()
        {
            for (int visibleIndex = 0; visibleIndex < subscriptionCount; visibleIndex++)
            {
                if (subscriptions[visibleIndex].visible)
                {
                    lastVisibility = true;
                    return;
                }
            }
            lastVisibility = false;
        }
        protected override bool CheckActive()
        {
            if (lastVisibility == false)
            {
                return false;
            }
            return base.CheckActive();
        }

    }

}
