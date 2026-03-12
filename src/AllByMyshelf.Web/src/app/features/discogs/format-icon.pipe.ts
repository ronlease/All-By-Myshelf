import { Pipe, PipeTransform } from '@angular/core';

/** Maps a Discogs format string to a Material icon name. */
@Pipe({ name: 'formatIcon', standalone: true })
export class FormatIconPipe implements PipeTransform {
  transform(format: string): string {
    const f = format.toLowerCase();
    if (f.includes('cassette') || f.includes('8-track') || f.includes('vhs')) return 'library_music';
    if (f.includes('blu-ray') || f.includes('bluray') || f.includes('dvd')) return 'movie';
    if (f.includes('box set')) return 'layers';
    if (f.includes('digital') || f.includes('file')) return 'music_note';
    if (f.includes('vinyl') || f.includes('cd') || f.includes('laserdisc')) return 'album';
    return 'music_note';
  }
}
