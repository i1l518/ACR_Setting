const { initializeApp, cert } = require('firebase-admin/app');
const { getFirestore } = require('firebase-admin/firestore');

// 1. ���� ���� Ű ���� �ε�
const serviceAccount = require('./amr-database-c3699-firebase-adminsdk-fbsvc-ae043dca98.json');

// 2. Firebase Admin �� �ʱ�ȭ
initializeApp({
  credential: cert(serviceAccount)
});

const db = getFirestore();

// 3. Ư�� �÷����� ��� ������ �����ϴ� �Լ�
async function deleteCollection(collectionPath, batchSize) {
  const collectionRef = db.collection(collectionPath);
  const query = collectionRef.orderBy('__name__').limit(batchSize);

  return new Promise((resolve, reject) => {
    deleteQueryBatch(query, resolve).catch(reject);
  });
}

async function deleteQueryBatch(query, resolve) {
  const snapshot = await query.get();

  // ������ ������ �� �̻� ������ ����
  if (snapshot.size === 0) {
    return resolve();
  }

  // 4. �ϰ�(Batch) ����� ���� ����
  const batch = db.batch();
  snapshot.docs.forEach((doc) => {
    batch.delete(doc.ref);
  });
  await batch.commit();

  // 5. ���� ������ �����ϱ� ���� ��������� �Լ� ȣ��
  process.nextTick(() => {
    deleteQueryBatch(query, resolve);
  });
}

// --- ���� ---
const collectionToDelete = 'tasks'; // ���⿡ ������ �÷��� �̸� �Է�
const batchSize = 500; // �� ���� ������ ���� �� (�ִ� 500)

console.log(`'${collectionToDelete}' �÷����� ��� ������ �����մϴ�...`);
deleteCollection(collectionToDelete, batchSize)
  .then(() => {
    console.log(`'${collectionToDelete}' �÷��� ���� �Ϸ�.`);
  })
  .catch((error) => {
    console.error("���� �� ���� �߻�: ", error);
  });