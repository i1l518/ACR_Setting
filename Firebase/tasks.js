// 1. 필요한 패키지 가져오기
const { initializeApp, cert } = require('firebase-admin/app');
const { getFirestore, Timestamp } = require('firebase-admin/firestore'); // Timestamp를 사용하기 위해 추가

// 2. 서비스 계정 키 파일 로드
const serviceAccount = require('./amr-database-c3699-firebase-adminsdk-fbsvc-ae043dca98.json');

// 3. Firebase Admin 앱 초기화
// 앱이 이미 초기화되지 않았을 경우에만 초기화하도록 방어 코드를 추가할 수 있습니다.
try {
  initializeApp({
    credential: cert(serviceAccount)
  });
} catch (error) {
  // 이미 초기화된 경우 에러가 발생할 수 있으므로 무시하거나 로그를 남길 수 있습니다.
  // console.log("Firebase Admin SDK가 이미 초기화되었습니다.");
}


// 4. Firestore 인스턴스 가져오기
const db = getFirestore();

// --- Task 문서 생성 함수 (새로 추가) ---
async function createTaskDocuments() {
    console.log('다중 운송 Task 문서 생성을 시작합니다...');
    const tasksCollectionRef = db.collection('tasks');

    // ==========================================================
    // 시나리오 1: 다중 입고 (Multi-Inbound) Task
    // ==========================================================
    // inbound_station_01에서 아이템 3개(A, B, C)를 순서대로 1, 2, 3번 슬롯에 싣고,
    // 각각 지정된 랙에 내려놓는 임무
    const multiInboundTask = {
        type: "multi_inbound",
        stops: [
            // 경유지 1: 입고 스테이션에서 아이템 3개 픽업
            {
                action: "pickup_multi", // 여러 개를 싣는다는 액션
                sourceStationId: "inbound_station_01",
                sourceStationRotation: { y: 180 },
                items_to_pickup: [ // 싣기로 약속된 아이템 목록
                    { itemType: "A", targetSlotId: 1 },
                    { itemType: "B", targetSlotId: 2 },
                    { itemType: "C", targetSlotId: 3 }
                ],
                status: "pending"
            },
            // 경유지 2: 1번 슬롯의 아이템을 Rack(2,0)에 놓기
            {
                action: "dropoff",
                sourceSlotId: 1, // "1번 슬롯에서 꺼내라"
                destination: {
                    rackId: "Rack(2,0)",
                    itemType: "A",
                    position: { x: 10, y: 1.5, z: 20 },
                    rotation: { y: 180 }
                },
                status: "pending"
            },
            // 경유지 3: 2번 슬롯의 아이템을 Rack(3,4)에 놓기
            {
                action: "dropoff",
                sourceSlotId: 2, // "2번 슬롯에서 꺼내라"
                destination: {
                    rackId: "Rack(3,4)",
                    itemType: "B",
                    position: { x: 15, y: 2.0, z: 25 },
                    rotation: { y: 180 }
                },
                status: "pending"
            },
            // 경유지 4: 3번 슬롯의 아이템을 Rack(4,7)에 놓기
            {
                action: "dropoff",
                sourceSlotId: 3, // "3번 슬롯에서 꺼내라"
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
    // 시나리오 2: 다중 출고 (Multi-Outbound) Task
    // ==========================================================
    // 랙 2개(Rack(2,1), Rack(3,8))에서 아이템을 각각 1, 2번 슬롯에 싣고,
    // 출고 스테이션(outbound_station_01)으로 운반하는 임무
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

    // --- 생성할 모든 Task를 배열에 담기 ---
    const allTasks = [multiInboundTask, multiOutboundTask];

    // --- 공통 필드 추가 및 문서 생성 ---
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
            console.log(`'${task.type}' Task '${docRef.id}' 문서 생성 성공!`);
        } catch (error) {
            console.error(`Task 문서 생성 중 오류 발생:`, error);
        }
    }
}

async function main() {
    await createTaskDocuments();
}

main();