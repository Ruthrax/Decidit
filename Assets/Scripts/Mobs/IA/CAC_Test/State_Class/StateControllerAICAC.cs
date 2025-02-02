using System.Collections.Generic;
using UnityEngine;

namespace State.AICAC
{
    public class StateControllerAICAC : MonoBehaviour
    {
        public _StateAICAC[] allStates;

        public enum AIState
        {
            BaseIdle, BaseMove, Dodge, BaseAttack, BaseDeath, SurroundPlayer
        }

        private Dictionary<AIState, _StateAICAC> stateDictionary = new Dictionary<AIState, _StateAICAC>();

        private _StateAICAC activeState;

        private Stack<AIState> stateHistory = new Stack<AIState>();

        void Start()
        {
            //Put all state into a dictionary
            foreach (_StateAICAC state in allStates)
            {
                if (state == null)
                {
                    continue;
                }

                //Inject a reference to this script into all states
                state.InitState(stateControllerAICAC: this);

                //Check if this key already exists, because it means we have forgotten to give a state its unique key
                if (stateDictionary.ContainsKey(state.state))
                {
                    Debug.LogWarning($"The key <b>{state.state}</b> already exists in the menu dictionary!");

                    continue;
                }

                stateDictionary.Add(state.state, state);
            }

            //Deactivate all states
            foreach (AIState state in stateDictionary.Keys)
            {
                try
                {
                    if (!stateDictionary[state].gameObject.activeInHierarchy)
                        Debug.LogWarning("Already Disable");
                    stateDictionary[state].gameObject.SetActive(false);
                }
                catch
                {
                }
            }

            //Activate the default state
            SetActiveState(AIState.BaseIdle);
        }

        //Activate a state
        public void SetActiveState(AIState newState, bool isJumpingBack = false)
        {
            //First check if this state exists
            if (!stateDictionary.ContainsKey(newState))
            {
                Debug.LogWarning($"The key <b>{newState}</b> doesn't exist so you can't activate the menu!");

                return;
            }

            //Deactivate the old state
            if (activeState != null)
            {
                activeState.gameObject.SetActive(false);
            }

            //Activate the new state
            activeState = stateDictionary[newState];

            activeState.gameObject.SetActive(true);

            //If we are jumping back we shouldn't add to history because then we will get doubles
            if (!isJumpingBack)
            {
                stateHistory.Push(newState);
            }
        }
    }
}