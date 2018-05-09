import { MediaTypeId } from "../enums/media-type-id";

export interface Movie {
    id: number;
    mediaTypeId: MediaTypeId;
    title: string;
    sourceId: number;
    summary: string;
    description: string;
    duration: number;
    rating?: string;
    releaseDate?: Date;
    runtime: number;
    tmdbId?: number;
    posterUrl: string;
    backdropUrls: string[];
    videoUrl: string;
}


