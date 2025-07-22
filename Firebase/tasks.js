// 1. �ʿ��� ��Ű�� ��������
const { initializeApp, cert } = require('firebase-admin/app');
const { getFirestore, Timestamp } = require('firebase-admin/firestore'); // Timestamp�� ����ϱ� ���� �߰�

// 2. ���� ���� Ű ���� �ε�
const serviceAccount = require('./amr-database-c3699-firebase-adminsdk-fbsvc-ae043dca98.json');

// 3. Firebase Admin �� �ʱ�ȭ
// ���� �̹� �ʱ�ȭ���� �ʾ��� ��쿡�� �ʱ�ȭ�ϵ��� ��� �ڵ带 �߰��� �� �ֽ��ϴ�.
try {
  initializeApp({
    credential: cert(serviceAccount)
  });
} catch (error) {
  // �̹� �ʱ�ȭ�� ��� ������ �߻��� �� �����Ƿ� �����ϰų� �α׸� ���� �� �ֽ��ϴ�.
  // console.log("Firebase Admin SDK�� �̹� �ʱ�ȭ�Ǿ����ϴ�.");
}


// 4. Firestore �ν��Ͻ� ��������
const db = getFirestore();

// --- Task ���� ���� �Լ� (���� �߰�) ---
async function createTaskDocuments() {
    console.log('���� ��� Task ���� ������ �����մϴ�...');
    const tasksCollectionRef = db.collection('tasks');

    // ==========================================================
    // �ó����� 1: ���� �԰� (Multi-Inbound) Task
    // ==========================================================
    // inbound_station_01���� ������ 3��(A, B, C)�� ������� 1, 2, 3�� ���Կ� �ư�,
    // ���� ������ ���� �������� �ӹ�
    const multiInboundTask = {
        type: "multi_inbound",
        stops: [
            // ������ 1: �԰� �����̼ǿ��� ������ 3�� �Ⱦ�
            {
                action: "pickup_multi", // ���� ���� �ƴ´ٴ� �׼�
                sourceStationId: "inbound_station_01",
                sourceStationRotation: { y: 180 },
                items_to_pickup: [ // �Ʊ�� ��ӵ� ������ ���
                    { itemType: "A", targetSlotId: 1 },
                    { itemType: "B", targetSlotId: 2 },
                    { itemType: "C", targetSlotId: 3 }
                ],
                status: "pending"
            },
            // ������ 2: 1�� ������ �������� Rack(2,0)�� ����
            {
                action: "dropoff",
                sourceSlotId: 1, // "1�� ���Կ��� ������"
                destination: {
                    rackId: "Rack(2,0)",
                    itemType: "A",
                    position: { x: 10, y: 1.5, z: 20 },
                    rotation: { y: 180 }
                },
                status: "pending"
            },
            // ������ 3: 2�� ������ �������� Rack(3,4)�� ����
            {
                action: "dropoff",
                sourceSlotId: 2, // "2�� ���Կ��� ������"
                destination: {
                    rackId: "Rack(3,4)",
                    itemType: "B",
                    position: { x: 15, y: 2.0, z: 25 },
                    rotation: { y: 180 }
                },
                status: "pending"
            },
            // ������ 4: 3�� ������ �������� Rack(4,7)�� ����
            {
                action: "dropoff",
                sourceSlotId: 3, // "3�� ���Կ��� ������"
                destination: {
                    rackId: "Rack(4,7)",
                    itemType: "C",
                    position: { x: 20, y: 2.5, z: 30 },
                    rotation: { y: 180 }
                },
                status: "pending"
            }
        ]
    };

    // ==========================================================
    // �ó����� 2: ���� ��� (Multi-Outbound) Task
    // ==========================================================
    // �� 2��(Rack(2,1), Rack(3,8))���� �������� ���� 1, 2�� ���Կ� �ư�,
    // ��� �����̼�(outbound_station_01)���� ����ϴ� �ӹ�
    const multiOutboundTask = {
        type: "multi_outbound",
        stops: [
            {
                action: "pickup",
                source: {
                    rackId: "Rack(2,1)", itemType: "A",
                    position: { x: 10, y: 1.5, z: 20 }, rotation: { y: 180 }
                },
                targetSlotId: 1,
                status: "pending"
            },
            {
                action: "pickup",
                source: {
                    rackId: "Rack(3,8)", itemType: "C",
                    position: { x: 15, y: 2.0, z: 25 }, rotation: { y: 180 }
                },
                targetSlotId: 2,
                status: "pending"
            },
            {
                action: "dropoff_multi",
                destinationStationId: "outbound_station_01",
                destinationStationRotation: { y: 0 },
                status: "pending"
            }
        ]
    };

    // --- ������ ��� Task�� �迭�� ��� ---
    const allTasks = [multiInboundTask, multiOutboundTask];

    // --- ���� �ʵ� �߰� �� ���� ���� ---
    for (const task of allTasks) {
        const taskData = {
            ...task,
            status: "pending",
            assignedAcrId: null,
            createdAt: Timestamp.now(),
            completedAt: null
        };

        try {
            const docRef = await tasksCollectionRef.add(taskData);
            console.log(`'${task.type}' Task '${docRef.id}' ���� ���� ����!`);
        } catch (error) {
            console.error(`Task ���� ���� �� ���� �߻�:`, error);
        }
    }
}

async function main() {
    await createTaskDocuments();
}

main();