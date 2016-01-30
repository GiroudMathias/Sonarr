var _ = require('underscore');
var vent = require('vent');
var Marionette = require('marionette');
var RenamePreviewCollection = require('./RenamePreviewCollection');
var RenamePreviewCollectionView = require('./RenamePreviewCollectionView');
var EmptyCollectionView = require('./RenamePreviewEmptyCollectionView');
var RenamePreviewFormatView = require('./RenamePreviewFormatView');
var LoadingView = require('Shared/LoadingView');
var CommandController = require('Commands/CommandController');

module.exports = Marionette.Layout.extend({
  template: 'Rename/RenamePreviewLayout',

  regions: {
    renamePreviews: '#rename-previews',
    formatRegion: '.x-format-region'
  },

  ui: {
    pathInfo: '.x-path-info',
    renameAll: '.x-rename-all',
    checkboxIcon: '.x-rename-all-button i'
  },

  events: {
    'click .x-organize': '_organizeFiles',
    'change .x-rename-all': '_toggleAll'
  },

  initialize(options) {
    this.model = options.series;
    this.seasonNumber = options.seasonNumber;

    var viewOptions = {};
    viewOptions.seriesId = this.model.id;
    viewOptions.seasonNumber = this.seasonNumber;

    this.collection = new RenamePreviewCollection(viewOptions);
    this.listenTo(this.collection, 'sync', this._showPreviews);
    this.listenTo(this.collection, 'rename:select', this._itemRenameChanged);

    this.collection.fetch();
  },

  onRender() {
    this.renamePreviews.show(new LoadingView());
    this.formatRegion.show(new RenamePreviewFormatView({ model: this.model }));
  },

  _showPreviews() {
    if (this.collection.length === 0) {
      this.ui.pathInfo.hide();
      this.renamePreviews.show(new EmptyCollectionView());
      return;
    }

    this.ui.pathInfo.show();
    this.collection.invoke('set', { rename: true });
    this.renamePreviews.show(new RenamePreviewCollectionView({ collection: this.collection }));
  },

  _organizeFiles() {
    if (this.collection.length === 0) {
      vent.trigger(vent.Commands.CloseFullscreenModal);
    }

    var files = _.map(this.collection.where({ rename: true }), function(model) {
      return model.get('episodeFileId');
    });

    if (files.length === 0) {
      vent.trigger(vent.Commands.CloseFullscreenModal);
      return;
    }

    if (this.seasonNumber) {
      CommandController.Execute('renameFiles', {
        name: 'renameFiles',
        seriesId: this.model.id,
        seasonNumber: this.seasonNumber,
        files: files
      });
    } else {
      CommandController.Execute('renameFiles', {
        name: 'renameFiles',
        seriesId: this.model.id,
        seasonNumber: -1,
        files: files
      });
    }

    vent.trigger(vent.Commands.CloseFullscreenModal);
  },

  _setCheckedState(checked) {
    if (checked) {
      this.ui.checkboxIcon.addClass('icon-sonarr-checked');
      this.ui.checkboxIcon.removeClass('icon-sonarr-unchecked');
    } else {
      this.ui.checkboxIcon.addClass('icon-sonarr-unchecked');
      this.ui.checkboxIcon.removeClass('icon-sonarr-checked');
    }
  },

  _toggleAll() {
    var checked = this.ui.renameAll.prop('checked');
    this._setCheckedState(checked);

    this.collection.each(function(model) {
      model.trigger('rename:select', model, checked);
    });
  },

  _itemRenameChanged(model, checked) {
    var allChecked = this.collection.all(function(m) {
      return m.get('rename');
    });

    if (!checked || allChecked) {
      this._setCheckedState(checked);
    }
  }
});