import { MediaTypeId } from "../enums/media-type-id";

export interface Source {
    id: number;
    folderPath: string;
    mediaTypeId: MediaTypeId;
}


