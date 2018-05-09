import { MediaTypeId } from "../enums/media-type-id";

export interface MediaItemHistoryRecord {
    mediaTypeId: MediaTypeId;
    posterUrl: string;
    title: string;
    runtimeSeconds: number;
    progressSecondsBegin: number;
    progressSecondsEnd: number;
    totalProgressSeconds: number;
    id: number;
    profileId: number;
    mediaItemId: number;
    dateBegin: string;
    dateEnd: string;
}

