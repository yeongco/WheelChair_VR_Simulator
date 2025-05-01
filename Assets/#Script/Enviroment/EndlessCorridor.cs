using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EndlessCorridor : MonoBehaviour
{
    [Header("Corridor Settings")]
    [SerializeField] private GameObject[] corridorSections;
    [SerializeField] private float sectionLength = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int numberOfSections = 5;
    [SerializeField] private Transform corridorParent;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    
    [Header("배치 설정")]
    [SerializeField] private Vector3 initialPosition = Vector3.zero; // 맵의 중앙 위치
    [SerializeField] private float sectionGap = 0f; // 섹션 사이의 간격
    [SerializeField] private Vector3 sectionOffset = Vector3.zero; // 각 섹션에 적용할 오프셋

    private List<GameObject> spawnedSections = new List<GameObject>();
    private Vector3 nextSpawnPosition;
    private float totalCorridorLength;

    private void Start()
    {
        if (corridorSections.Length == 0)
        {
            Debug.LogError("No corridor sections assigned!");
            return;
        }

        // 이동 방향 정규화
        moveDirection = moveDirection.normalized;
        
        // 전체 통로 길이 계산 (간격 포함)
        totalCorridorLength = (sectionLength + sectionGap) * numberOfSections;
        
        // 초기 생성 위치 설정 - initialPosition을 중심으로 뒤쪽부터 시작
        nextSpawnPosition = initialPosition - (moveDirection * (totalCorridorLength / 2));
        
        // 초기 통로 섹션 생성
        for (int i = 0; i < numberOfSections; i++)
        {
            SpawnCorridorSection();
        }
        
        // 통로 이동 시작
        StartCoroutine(MoveCorridorEndlessly());
    }

    private void SpawnCorridorSection()
    {
        // 랜덤 통로 섹션 프리팹 선택
        GameObject sectionPrefab = corridorSections[Random.Range(0, corridorSections.Length)];
        
        // 섹션 생성 (X축으로 -90도 회전 적용)
        Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
        GameObject newSection = Instantiate(sectionPrefab, nextSpawnPosition + sectionOffset, rotation);
        
        // 통로 부모 오브젝트가 할당되어 있으면 부모로 설정
        if (corridorParent != null)
        {
            newSection.transform.parent = corridorParent;
        }
        
        // 목록에 추가
        spawnedSections.Add(newSection);
        
        // 다음 생성 위치 업데이트 (섹션 길이 + 간격)
        nextSpawnPosition += moveDirection * (sectionLength + sectionGap);
    }

    private IEnumerator MoveCorridorEndlessly()
    {
        // 섹션들의 이동 순서를 저장할 리스트
        List<GameObject> sectionsInOrder = new List<GameObject>();
        float checkTime = 0f; // 디버그 로그 출력 시간 제한용
        
        while (true)
        {
            float deltaMovement = moveSpeed * Time.deltaTime;
            checkTime += Time.deltaTime;
            
            // 먼저 모든 섹션 이동
            foreach (GameObject section in spawnedSections)
            {
                // 플레이어 이동 방향의 반대로 섹션 이동
                section.transform.position -= moveDirection * deltaMovement;
            }
            
            // 각 섹션의 위치를 체크하고 재배치
            bool anyRepositioned = false;
            
            // 각 섹션에 대해 독립적으로 처리
            for (int i = 0; i < spawnedSections.Count; i++)
            {
                GameObject section = spawnedSections[i];
                
                // 중앙점(initialPosition)으로부터의 위치 벡터
                Vector3 positionVector = section.transform.position - initialPosition;
                
                // 이동 방향에 투영된 상대적 위치 (스칼라값)
                float relativePosition = Vector3.Dot(positionVector, moveDirection);
                
                // 주기적으로 위치 정보 출력 (디버깅용)
                if (checkTime >= 1.0f && i == 0)
                {
                    //Debug.Log($"Section[{i}] relative pos: {relativePosition}, threshold: {totalCorridorLength / 2}");
                    checkTime = 0f;
                }
                
                // 앞쪽으로 일정 범위를 벗어나면 뒤쪽으로 재배치
                if (relativePosition > totalCorridorLength / 2)
                {
                    //Debug.Log($"Repositioning section {i}, rel pos: {relativePosition}");
                    RepositionSectionToBack(section);
                    anyRepositioned = true;
                }
                // 뒤쪽으로 일정 범위를 벗어나면 앞쪽으로 재배치 (반대 방향도 체크)
                else if (relativePosition < -totalCorridorLength / 2)
                {
                    //Debug.Log($"Repositioning section {i} from back, rel pos: {relativePosition}");
                    RepositionSectionToFront(section);
                    anyRepositioned = true;
                }
            }
            
            // 재배치된 섹션이 있으면 간격 유지 로직 실행
            if (anyRepositioned)
            {
                MaintainSectionSpacing();
            }
            
            yield return null;
        }
    }

    // 섹션을 맵의 뒤쪽 끝으로 재배치
    private void RepositionSectionToBack(GameObject section)
    {
        // 모든 섹션 중에서 가장 뒤쪽에 있는 섹션 찾기
        GameObject farthestBackSection = null;
        float minRelativePos = float.MaxValue;
        
        foreach (GameObject otherSection in spawnedSections)
        {
            if (otherSection == section) continue;
            
            float relPos = Vector3.Dot(otherSection.transform.position - initialPosition, moveDirection);
            if (relPos < minRelativePos)
            {
                minRelativePos = relPos;
                farthestBackSection = otherSection;
            }
        }
        
        // 가장 뒤쪽 섹션이 존재하면 그 뒤에 배치
        if (farthestBackSection != null)
        {
            Vector3 basePos = farthestBackSection.transform.position - sectionOffset; // 오프셋 제외한 위치
            Vector3 newPosition = basePos - moveDirection * (sectionLength + sectionGap);
            section.transform.position = newPosition + sectionOffset;
            //Debug.Log($"Repositioned to back: {section.name} at pos {newPosition}");
        }
        else
        {
            // 다른 섹션이 없는 경우 맵 중앙에서 뒤쪽으로 이동
            Vector3 newPosition = initialPosition - moveDirection * (totalCorridorLength / 2);
            section.transform.position = newPosition + sectionOffset;
            //Debug.Log($"Repositioned to center-back: {section.name}");
        }
    }

    // 섹션을 맵의 앞쪽 끝으로 재배치
    private void RepositionSectionToFront(GameObject section)
    {
        // 모든 섹션 중에서 가장 앞쪽에 있는 섹션 찾기
        GameObject farthestFrontSection = null;
        float maxRelativePos = float.MinValue;
        
        foreach (GameObject otherSection in spawnedSections)
        {
            if (otherSection == section) continue;
            
            float relPos = Vector3.Dot(otherSection.transform.position - initialPosition, moveDirection);
            if (relPos > maxRelativePos)
            {
                maxRelativePos = relPos;
                farthestFrontSection = otherSection;
            }
        }
        
        // 가장 앞쪽 섹션이 존재하면 그 앞에 배치
        if (farthestFrontSection != null)
        {
            Vector3 basePos = farthestFrontSection.transform.position - sectionOffset; // 오프셋 제외한 위치
            Vector3 newPosition = basePos + moveDirection * (sectionLength + sectionGap);
            section.transform.position = newPosition + sectionOffset;
            //Debug.Log($"Repositioned to front: {section.name} at pos {newPosition}");
        }
        else
        {
            // 다른 섹션이 없는 경우 맵 중앙에서 앞쪽으로 이동
            Vector3 newPosition = initialPosition + moveDirection * (totalCorridorLength / 2);
            section.transform.position = newPosition + sectionOffset;
            //Debug.Log($"Repositioned to center-front: {section.name}");
        }
    }

    // 간격 유지 함수 - 코드 분리로 가독성 향상
    private void MaintainSectionSpacing()
    {
        if (spawnedSections.Count <= 1) return;
        
        // 섹션들의 상대적 위치에 따라 순서 정렬
        List<GameObject> sectionsInOrder = new List<GameObject>(spawnedSections);
        
        // 이동 방향을 기준으로 섹션들을 정렬 (앞에서 뒤로)
        sectionsInOrder.Sort((a, b) => {
            float posA = Vector3.Dot(a.transform.position - initialPosition, moveDirection);
            float posB = Vector3.Dot(b.transform.position - initialPosition, moveDirection);
            return posA.CompareTo(posB);
        });
        
        // 간격 유지를 위한 검사 및 조정 - 앞에서부터 차례로 처리하여 오류 전파 방지
        for (int i = 0; i < sectionsInOrder.Count - 1; i++)
        {
            GameObject current = sectionsInOrder[i];
            GameObject next = sectionsInOrder[i + 1];
            
            // 현재 섹션과 다음 섹션 사이의 거리 계산
            Vector3 currPos = current.transform.position - sectionOffset; // 오프셋 제외한 실제 위치
            Vector3 nextPos = next.transform.position - sectionOffset;
            
            float distance = Vector3.Dot(nextPos - currPos, moveDirection);
            float expectedDistance = sectionLength + sectionGap;
            
            // 간격이 예상과 다르면 조정 (일정 임계값 이상 차이날 때만)
            if (Mathf.Abs(distance - expectedDistance) > 0.05f)
            {
                // 다음 섹션 위치 조정
                Vector3 correctPosition = currPos + moveDirection * expectedDistance + sectionOffset;
                next.transform.position = correctPosition;
            }
        }
    }

    // DOTween을 사용한 대체 이동 방법
    public void StartCorridorMovementWithDOTween()
    {
        StopAllCoroutines();
        
        foreach (GameObject section in spawnedSections)
        {
            // 기존 트윈 제거
            DOTween.Kill(section.transform);
            
            // 무한 이동 생성
            MoveSectionWithDOTween(section);
        }
    }
    
    private void MoveSectionWithDOTween(GameObject section)
    {
        // 목표 위치 계산 (한 섹션 길이+간격만큼 앞으로)
        Vector3 targetPos = section.transform.position - moveDirection * (sectionLength + sectionGap);
        
        // 트윈 생성
        section.transform.DOMove(targetPos, (sectionLength + sectionGap) / moveSpeed)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                // 중앙점(initialPosition)으로부터의 상대적 위치 계산
                Vector3 positionVector = section.transform.position - initialPosition;
                float relativePosition = Vector3.Dot(positionVector, moveDirection);
                
                // 앞쪽으로 일정 범위를 벗어나면 뒤쪽으로 재배치
                if (relativePosition > totalCorridorLength / 2)
                {
                    //Debug.Log($"DOTween - Repositioning to back, rel pos: {relativePosition}");
                    RepositionSectionToBack(section);
                }
                // 뒤쪽으로 일정 범위를 벗어나면 앞쪽으로 재배치 (반대 방향도 체크)
                else if (relativePosition < -totalCorridorLength / 2)
                {
                    //Debug.Log($"DOTween - Repositioning to front, rel pos: {relativePosition}");
                    RepositionSectionToFront(section);
                }
                
                // 위치 조정 후 간격 검사 및 보정
                MaintainSectionSpacing();
                
                // 이동 계속
                MoveSectionWithDOTween(section);
            });
    }

    // 기존 ForceCheckAndRepositionSections 함수 업데이트
    public void ForceCheckAndRepositionSections()
    {
        //Debug.Log("Force checking all sections...");
        
        for (int i = 0; i < spawnedSections.Count; i++)
        {
            GameObject section = spawnedSections[i];
            Vector3 positionVector = section.transform.position - initialPosition;
            float relativePosition = Vector3.Dot(positionVector, moveDirection);
            
            //Debug.Log($"Section[{i}] relative pos: {relativePosition}, threshold: {totalCorridorLength / 2}");
            
            if (relativePosition > totalCorridorLength / 2)
            {
                //Debug.Log($"Force repositioning section {i} to back");
                RepositionSectionToBack(section);
            }
            else if (relativePosition < -totalCorridorLength / 2)
            {
                //Debug.Log($"Force repositioning section {i} to front");
                RepositionSectionToFront(section);
            }
        }
        
        MaintainSectionSpacing();
        //Debug.Log("Force repositioning complete.");
    }

    // 섹션 초기화를 위한 위치 설정 함수 추가
    public void ResetSectionPositions()
    {
        //Debug.Log("Resetting all section positions...");
        
        // 모든 이동 중지
        StopAllCoroutines();
        
        // 초기 생성 위치 재설정
        nextSpawnPosition = initialPosition - (moveDirection * (totalCorridorLength / 2));
        
        // 기존 섹션 제거
        foreach (GameObject section in spawnedSections)
        {
            Destroy(section);
        }
        
        spawnedSections.Clear();
        
        // 초기 통로 섹션 생성
        for (int i = 0; i < numberOfSections; i++)
        {
            SpawnCorridorSection();
        }
        
        // 통로 이동 시작
        StartCoroutine(MoveCorridorEndlessly());
        
        //Debug.Log("All sections have been reset.");
    }
} 