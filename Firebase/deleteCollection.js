const { initializeApp, cert } = require('firebase-admin/app');
const { getFirestore } = require('firebase-admin/firestore');

// 1. 서비스 계정 키 파일 로드
const serviceAccount = require('./amr-database-c3699-firebase-adminsdk-fbsvc-ae043dca98.json');

// 2. Firebase Admin 앱 초기화
initializeApp({
  credential: cert(serviceAccount)
});

const db = getFirestore();

// 3. 특정 컬렉션의 모든 문서를 삭제하는 함수
async function deleteCollection(collectionPath, batchSize) {
  const collectionRef = db.collection(collectionPath);
  const query = collectionRef.orderBy('__name__').limit(batchSize);

  return new Promise((resolve, reject) => {
    deleteQueryBatch(query, resolve).catch(reject);
  });
}

async function deleteQueryBatch(query, resolve) {
  const snapshot = await query.get();

  // 삭제할 문서가 더 이상 없으면 종료
  if (snapshot.size === 0) {
    return resolve();
  }

  // 4. 일괄(Batch) 쓰기로 문서 삭제
  const batch = db.batch();
  snapshot.docs.forEach((doc) => {
    batch.delete(doc.ref);
  });
  await batch.commit();

  // 5. 다음 묶음을 삭제하기 위해 재귀적으로 함수 호출
  process.nextTick(() => {
    deleteQueryBatch(query, resolve);
  });
}

// --- 실행 ---
const collectionToDelete = 'tasks'; // 여기에 삭제할 컬렉션 이름 입력
const batchSize = 500; // 한 번에 삭제할 문서 수 (최대 500)

console.log(`'${collectionToDelete}' 컬렉션의 모든 문서를 삭제합니다...`);
deleteCollection(collectionToDelete, batchSize)
  .then(() => {
    console.log(`'${collectionToDelete}' 컬렉션 삭제 완료.`);
  })
  .catch((error) => {
    console.error("삭제 중 오류 발생: ", error);
  });