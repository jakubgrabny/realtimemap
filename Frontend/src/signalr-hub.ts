import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';
import { notify } from "@kyvg/vue3-notification";

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/positionhub')
  .configureLogging(LogLevel.Debug)
  .build();

export interface HubMessageDto {
  payload: HubMessagePayload;
  data: any;
}

export interface HubMessagePayload {
  messageType: HubMessageType;
}

export enum HubMessageType {
  Position = 1,
  Notification = 2
}

export interface PositionsDto {
  positions: PositionDto[];
}
export interface PositionDto {
  vehicleId: string;
  longitude: number;
  latitude: number;
  heading: number;
  speed: number;
  doorsOpen: boolean;
}

export default {
  async connect(
    lng1: number,
    lat1: number,
    lng2: number,
    lat2: number,
    callback: (value: any) => void
  ) {
    console.log('connecting');
    await connection.start();
    console.log('connected');

    connection.onclose(() => {
      console.log('connection closed');
    });

    connection.on("notification", (notification: string) => {
      console.log(notification);
      notify({
        title: "GeoFence alert",
        text: notification,
        duration: 10000
      });
    });

    connection.stream('Connect').subscribe({
      next: (hubMessage: PositionsDto) => {
        console.log(`Got batch of events ${hubMessage.positions.length}`);
        for (const e of hubMessage.positions) {
          callback(e);
        }
      },
      complete: () => {
        //pass
        console.log('signalr completed');
      },
      error: x => {
        //pass
        console.error('signalr error', x);
      },
    });
  },

  async setViewport(
    swLng: number,
    swLat: number,
    neLng: number,
    neLat: number
  ) {
    await connection.send('SetViewport', swLng, swLat, neLng, neLat);
  },
};
