(function () {
  if (window.__cloudIslandV3 && window.__cloudIslandV3.version === 4) return true;
  if (window.__cloudIslandV3) {
    installBackgroundPlay(window.__cloudIslandV3);
    installSearch(window.__cloudIslandV3, window.__cloudIslandRequire);
    window.__cloudIslandV3.version = 4;
    return true;
  }
  function installBackgroundPlay(api) {
    api.play = async function (id) {
      if (!/^\d+$/.test(String(id))) throw new Error('歌曲 ID 无效');
      if (api.state().id === String(id)) {
        if (!api.state().playing) await api.app.getDispatch()({type: 'playing/resume', payload: {triggerScene: 'unknown'}});
      } else {
        await api.app.getDispatch()({type: 'async:action/doAction', payload: {
          actionId: 'play', data: {resource: {id: String(id)}, resourceType: 'track',
            from: {scene: 'unknown', needOpenVinylPage: false}}, eventType: 'click'
        }});
      }
      const started = Date.now();
      while (Date.now() - started < 7500) {
        const state = api.state();
        if (state.id === String(id) && state.playing) return state;
        await new Promise(resolve => setTimeout(resolve, 150));
      }
      throw new Error('网易云未确认播放，请检查会员、版权或网络状态');
    };
  }
  if (!window.__cloudIslandRequire) {
    if (!Array.isArray(window.webpackJsonp)) throw new Error('当前网易云版本不支持本机控制适配');
    webpackJsonp.push([[999071], {999071: function (module, exports, require) {window.__cloudIslandRequire = require;}}, [[999071]]]);
  }
  const require = window.__cloudIslandRequire;
  let app, player;
  for (const id of Object.keys(require.c)) {
    const exports = require.c[id].exports;
    if (!exports) continue;
    if (exports.AudioPlayer && typeof exports.AudioPlayer.subscribePlayStatus === 'function') player = exports.AudioPlayer;
    for (const key of Object.keys(exports)) {
      let value;
      try { value = exports[key]; } catch (_) {continue;}
      if (value && typeof value.getStore === 'function' && typeof value.getDispatch === 'function') app = value;
    }
  }
  if (!app || !player) throw new Error('网易云播放接口尚未就绪');
  const api = {app, player, clock: null, seekEvent: null};
  player.subscribePlayStatus({type: 'playprogress', callback: event => {
    const pos = Number(event.current);
    if (Number.isFinite(pos) && pos >= 0) api.clock = {playId: String(event.playId), position: pos, stamp: Date.now()};
  }});
  player.subscribePlayStatus({type: 'seek', callback: event => {
    const pos = Number(event.position);
    if (event.code === 0 && Number.isFinite(pos) && pos >= 0) {
      api.clock = {playId: String(event.playId), position: pos, stamp: Date.now()};
      api.seekEvent = {playId: String(event.playId), position: pos, stamp: Date.now()};
    }
  }});
  api.state = function () {
    const state = app.getStore().playing;
    const item = state.curPlaying || {};
    const track = item.track || state.curTrack || {};
    const artists = track.artists || track.ar || state.resourceArtists || [];
    const album = track.album || track.al || {};
    const clock = api.clock && api.clock.playId === String(state.playId) ? api.clock : null;
    const duration = Number(state.resourceDuration) || Number(track.duration || track.dt) / 1000 || 0;
    return {connected: true, hasTimeline: !!clock, playing: state.playingState === 2,
      id: String(item.resourceId || track.id || ''), title: track.name || state.resourceName || '',
      artist: artists.map(a => a.name || '').join('/'), duration,
      position: clock ? Math.min(duration, clock.position) : 0,
      sampleAge: clock ? Math.max(0, (Date.now() - clock.stamp) / 1000) : 0,
      cover: album.picUrl || state.resourceCoverUrl || '', source: 'native', speed: Number(state.playingSpeed) || 1};
  };
  api.seek = async function (seconds, expectedId) {
    const before = api.state();
    if (expectedId && before.id !== String(expectedId)) throw new Error('歌曲已切换，请重新拖动进度');
    if (!Number.isFinite(seconds) || seconds < 0 || !before.duration) throw new Error('当前歌曲不能跳转进度');
    const target = Math.max(0, Math.min(before.duration - 0.1, seconds));
    const started = Date.now();
    await app.getDispatch()({type: 'playing/setPlayingPosition', payload: {duration: target}});
    while (Date.now() - started < 3500) {
      const after = api.state();
      if (after.id !== before.id) throw new Error('跳转时歌曲已切换');
      if (after.hasTimeline && Math.abs(after.position - target) < 1.5 && api.clock.stamp >= started) return after;
      await new Promise(resolve => setTimeout(resolve, 80));
    }
    throw new Error('网易云未确认进度跳转，请稍后重试');
  };
  function installSearch(api, require) {
   api.search = async function (query, offset) {
    if (!api.searchRequest) {
      const path = '/api/cloudsearch/pc';
      for (const id of Object.keys(require.m)) {
        const source = require.m[id].toString();
        const index = source.indexOf('"' + path + '"');
        if (index < 0) continue;
        const prefix = source.slice(Math.max(0, index - 400), index);
        const assignments = [...prefix.matchAll(/([A-Za-z_$][\w$]*)=/g)];
        if (!assignments.length) continue;
        const name = assignments[assignments.length - 1][1];
        const escaped = name.replace(/[$]/g, '\\$');
        const mapping = source.match(new RegExp('\\.d\\(\\w+,"([^"]+)",\\(function\\(\\)\\{return ' + escaped + '\\}\\)\\)'));
        if (mapping && typeof require(id)[mapping[1]] === 'function') {api.searchRequest = require(id)[mapping[1]]; break;}
      }
    }
    if (!api.searchRequest) throw new Error('当前网易云版本的搜索接口尚未适配');
    const response = await api.searchRequest({s: String(query).slice(0, 200), type: 1, offset: offset || 0, limit: 30, total: true});
    const result = response.result || {};
    if (!Array.isArray(result.songs)) {if (Number(result.songCount) === 0) return {songs: [], total: 0}; throw new Error('网易云未返回有效搜索结果');}
    return {songs: result.songs.map(track => ({id: String(track.id), name: track.name,
      artist: (track.ar || track.artists || []).map(a => a.name).join(' / '),
      album: (track.al || track.album || {}).name || '', cover: (track.al || track.album || {}).picUrl || '', duration: Number(track.dt || track.duration) / 1000 || 0,
      fee: Number(track.fee) || 0})), total: Number(result.songCount) || result.songs.length};
  };
  }
  installSearch(api, require);
  installBackgroundPlay(api);
  api.version = 4;
  window.__cloudIslandV3 = api;
  return true;
})()
