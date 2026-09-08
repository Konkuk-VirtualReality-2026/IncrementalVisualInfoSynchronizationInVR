using UnityEngine;

public class SetBoolOnState : StateMachineBehaviour
{
    [Header("Settings")]
    [Tooltip("제어할 Animator의 Bool 파라미터 정확한 이름을 적으세요.")]
    public string boolParameterName = "IsUnstunnable";

    [Tooltip("이 상태에 진입할 때(Enter) True로 설정할까요? (체크 해제 시 False)")]
    public bool targetValueOnEnter = true;

    [Tooltip("이 상태에서 나갈 때(Exit) 값을 원상복구(반대값) 할까요?")]
    public bool resetOnExit = true;

    // 상태에 진입할 때 실행됨
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 예: IsUnstunnable을 true로 만듦
        animator.SetBool(boolParameterName, targetValueOnEnter);
    }

    // 상태에서 빠져나갈 때 실행됨
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (resetOnExit)
        {
            // 예: IsUnstunnable을 false로 만듦 (들어올 때 값의 반대)
            animator.SetBool(boolParameterName, !targetValueOnEnter);
        }
    }
}