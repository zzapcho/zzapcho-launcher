const { contextBridge, ipcRenderer, webUtils } = require('electron');

contextBridge.exposeInMainWorld('launcher', {
  // Auth
  login:     () => ipcRenderer.invoke('auth:login'),
  checkAuth: () => ipcRenderer.invoke('auth:check'),
  logout:    () => ipcRenderer.invoke('auth:logout'),

  // Presets
  listPresets: () => ipcRenderer.invoke('presets:list'),

  // Setup
  runSetup:        (manifest) => ipcRenderer.invoke('setup:run', manifest),
  onSetupProgress: (cb) => ipcRenderer.on('setup:progress', (_, d) => cb(d)),

  // Update check (presetId 전달)
  checkUpdate: (presetId) => ipcRenderer.invoke('update:check', presetId),

  // Game
  launch:           () => ipcRenderer.invoke('game:launch'),
  onProgress:       (cb) => ipcRenderer.on('game:progress',       (_, d) => cb(d)),
  onDownloadStatus: (cb) => ipcRenderer.on('game:download-status', (_, d) => cb(d)),
  onGameClosed:     (cb) => ipcRenderer.on('game:closed',          (_, c) => cb(c)),
  onGameLog:        (cb) => ipcRenderer.on('game:log',             (_, e) => cb(e)),

  // File management
  listFiles:      (category)             => ipcRenderer.invoke('files:list', category),
  addFiles:       (category, paths)      => ipcRenderer.invoke('files:add', category, paths),
  removeFile:     (category, fileName)   => ipcRenderer.invoke('files:remove', category, fileName),
  openFileDialog: (category)             => ipcRenderer.invoke('files:open-dialog', category),
  openGameFolder: ()                     => ipcRenderer.invoke('folder:open-game'),
  getServerStatus: ()                    => ipcRenderer.invoke('server:status'),

  // Settings
  getSettings: ()  => ipcRenderer.invoke('settings:get'),
  setSettings: (s) => ipcRenderer.invoke('settings:set', s),
  getVersion:  ()  => ipcRenderer.invoke('app:version'),

  // Auto-updater events
  onUpdaterAvailable: (cb) => ipcRenderer.on('updater:available', (_, v) => cb(v)),
  onUpdaterProgress:  (cb) => ipcRenderer.on('updater:progress',  (_, p) => cb(p)),
  onUpdaterDownloaded:(cb) => ipcRenderer.on('updater:downloaded', (_, v) => cb(v)),

  // Window
  minimize: () => ipcRenderer.send('window:minimize'),
  close:    () => ipcRenderer.send('window:close'),

  // File path (drag & drop)
  getPathForFile: (file) => webUtils.getPathForFile(file)
});
