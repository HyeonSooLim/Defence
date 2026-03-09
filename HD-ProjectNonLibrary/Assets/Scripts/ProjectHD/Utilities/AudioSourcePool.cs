using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utilities
{
    public class AudioSourcePool
    {
        private readonly AudioSource prefab;

        private readonly Queue<AudioSource> audioSources = new Queue<AudioSource>(); // 사용 가능한 오디오 소스 큐
        private readonly LinkedList<AudioSource> inuse = new LinkedList<AudioSource>(); // 사용 중인 오디오 소스 리스트
        private readonly Queue<LinkedListNode<AudioSource>> nodePool = new Queue<LinkedListNode<AudioSource>>(); // 사용 가능한 노드 큐

        private int lastCheckFrame = -1; // 마지막으로 확인한 프레임
        private const int MaxAudioSources = 10; // 최대 오디오 소스 개수

        public AudioSourcePool(AudioSource prefab)
        {
            this.prefab = prefab;
            SceneManager.sceneUnloaded += SceneUnloaded; // 씬 언로드 이벤트 등록
        }

        private void SceneUnloaded(Scene scene)
        {
            audioSources?.Clear(); // 오디오 소스 큐 초기화
            inuse?.Clear(); // 사용 중인 오디오 소스 리스트 초기화
            nodePool?.Clear(); // 노드 큐 초기화        
        }

        private void CheckInUse()
        {
            var node = inuse.First;
            while (node != null)
            {
                var current = node;
                node = node.Next;

                if (!current.Value.isPlaying)
                {
                    audioSources.Enqueue(current.Value); // 재생이 끝난 오디오 소스를 큐에 추가
                    inuse.Remove(current); // 사용 중인 리스트에서 제거
                    nodePool.Enqueue(current); // 노드 큐에 추가
                }
            }
        }

        public void PlayAtPoint(AudioClip clip, Vector3 point)
        {
            AudioSource source;

            if (lastCheckFrame != Time.frameCount)
            {
                lastCheckFrame = Time.frameCount;
                CheckInUse(); // 사용 중인 오디오 소스 확인
            }

            if (audioSources.Count == 0)
                source = GameObject.Instantiate(prefab); // 새로운 오디오 소스 인스턴스 생성
            else
                source = audioSources.Dequeue(); // 사용 가능한 오디오 소스 가져오기

            if (nodePool.Count == 0)
                inuse.AddLast(source); // 사용 중인 리스트에 추가
            else
            {
                var node = nodePool.Dequeue();
                node.Value = source;
                inuse.AddLast(node); // 사용 중인 리스트에 노드 추가
            }

            source.transform.position = point; // 오디오 소스 위치 설정
            source.clip = clip; // 오디오 클립 설정
            source.Play(); // 오디오 재생
        }

        public void PlaySFX(AudioClip clip)
        {
            if (lastCheckFrame != Time.frameCount)
            {
                lastCheckFrame = Time.frameCount;
                CheckInUse(); // 사용 중인 오디오 소스 확인
            }

            if (inuse.Count >= MaxAudioSources) // 최신 상태의 inuse 리스트를 기준으로 최대 오디오 소스 개수 체크
            {
                Debug.LogWarning("Maximum audio sources reached. Skipping SFX playback.");
                return;
            }

            AudioSource source;

            if (audioSources.Count == 0)
                source = GameObject.Instantiate(prefab); // 새로운 오디오 소스 인스턴스 생성
            else
                source = audioSources.Dequeue(); // 사용 가능한 오디오 소스 가져오기

            if (nodePool.Count == 0)
                inuse.AddLast(source); // 사용 중인 리스트에 추가
            else
            {
                var node = nodePool.Dequeue();
                node.Value = source;
                inuse.AddLast(node); // 사용 중인 리스트에 노드 추가
            }

            source.transform.position = Vector3.zero; // 오디오 소스 위치 초기화
            source.clip = clip; // 오디오 클립 설정
            source.Play(); // 오디오 재생
        }
    }
}