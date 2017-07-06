using UnityEngine;
using System;

namespace Framework
{
	using StateMachineSystem;
	using TimelineSystem;
	using Utils;

	namespace TimelineStateMachineSystem
	{
		[Serializable]
		[EventCategory("Flow")]
		public class EventSetComponentEnabled : Event, IStateMachineEvent
		{
			#region Public Data
			public ComponentRef<MonoBehaviour> _target = new ComponentRef<MonoBehaviour>();
			public bool _enabled = false;
			#endregion

			#region Event
#if UNITY_EDITOR
			public override Color GetColor()
			{
				return new Color(0.3f, 0.6f, 0.8f);
			}

			public override string GetEditorDescription()
			{
				return (_enabled ? "Enable" : "Disable") + " Component (<b>"+_target+"</b>)";
			}
#endif
			#endregion

			#region IStateMachineSystemEvent
			public eEventTriggerReturn Trigger(StateMachine stateMachine)
			{
				MonoBehaviour target = _target.GetComponent();

				if (target != null)
				{
					target.enabled = _enabled;
				}

				return eEventTriggerReturn.EventFinished;
			}

			public eEventTriggerReturn Update(StateMachine stateMachine, float eventTime)
			{
				return eEventTriggerReturn.EventOngoing;
			}

			public void End(StateMachine stateMachine) { }
#if UNITY_EDITOR
			public EditorStateLink[] GetEditorLinks() { return null; }
#endif
			#endregion
		}
	}
}
