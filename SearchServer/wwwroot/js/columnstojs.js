(function($) {


	$.fn.columnsToJs = function(options) {
        var _this = this,
			j_this =  $(_this)
        //parametry wejściowe
      	_this = $.extend( {
         	count: 3,
         	check: 0,
         	gap: 20,
         	fixed: true,
         	debug: false,
         	interval: false
        }, options);


      	// Możliwosc więcej niż jednej kolumny na stronie
      	this.each(function() {

			_this.child = j_this.children();

	 		if(_this.check == "all"){
	 			_this.check = _this.child.length;
	 		}
	        if(_this.check < _this.count + 2){
	        	_this.check = _this.count + 2;
	        }

			_this.bodywidth = $(window).width();

			_this.ifInPostiton = function(left, top, lastPostion){
				var ifPosible = true;
				for (var i = lastPostion.length - 1; i >= 0; i--) {
					// console.log("ifInPostiton i                 : "+i);
					// console.log("ifInPostiton max               : "+max);
					// console.log("ifInPostiton lastPostion.length: "+lastPostion.length);
					if(lastPostion[i][0] == top && lastPostion[i][1] == left){
						return false
					}
				}
				return true
			}


			$(_this.child).css('transition', 'none');
			_this.init = function(){
      			_this.gapNumber = _this.count - 1;
				_this.parentWidth = j_this.width();
				_this.rowWidth = (_this.parentWidth - _this.gapNumber * _this.gap ) / _this.count;
		        _this.getBest = 0;
				j_this.css({
					'column-count': '1',
					'column-gap': '0',
					'position': 'relative'
				});
				children = j_this.children().filter(":visible");
				if(_this.debug){
					console.log("INIT!!!");
					console.log(_this);
				}
				var landigLeft = 0;
				var lastPostion = [];
				
				var memorycalc = [];

		        children.each(function() {
		        	var element = $(this),
		        		position = [0,0], // TOP, LEFT
		        		index = element.filter(":visible").index();

		        	var elementMemoru = memorycalc[index];



					// Nadaj szerokość css
					// musi być wywołana wcześnie bo ma wpływ na wysokość elementu
		        	element.css({
						width: _this.rowWidth+'px',
					});



		        	if(_this.fixed){
		        		// Ustaw według jak najbardziej optymalnie

		        		// Ustaw pierwszy rząd po staremu
						if(index>_this.count-1) {
							var topPositon = -1;
							var leftPositon = -50;
			        		// console.log(index);
			        		// position[0] = brotherTop.position().top + brotherTop.outerHeight(true);
			        		if(_this.debug){
			        			children.css('background', '');
			        		}
			        		var check = _this.check;
			        		if(check>index){
			        			check=index;
			        		}
			        		for (var i = index; i >= index - check +1; i--) {


			        			var forI = Math.abs(i-1);
			        			var brother = children.filter(":visible").eq(forI);
			        			
			        			if(memorycalc[forI]){
				        			// console.log("AAAAAAAAAAAA");
				        			if(_this.debug){
				        				console.log(forI+" szukam na poziomie "+index);
				        			}
				        			

				        			var brotherTop = memorycalc[forI].top+brother.outerHeight(true);
				        			var brotherLeft = memorycalc[forI].left;

									// var brotherTop = 0;
				        			// var brotherLeft = 0;

					        		if(_this.debug){
						        		console.log("brotherTop: ", brotherTop);
						        		console.log("brotherLeft: ", brotherLeft);
					        			element.css('background', 'pink');
					        			brother.css('background', 'red');
				        			}
				        			if(topPositon == -1 || brotherTop<=topPositon){
				        				if(_this.ifInPostiton(brotherLeft,brotherTop,lastPostion)){
											if(_this.debug){
					        					console.log("USTAW");
					        					brother.css('background', 'blue');
					        				}
				        					topPositon = brotherTop;
											leftPositon = brotherLeft;
				        				}
				        			}
				        		}
			        			
			        		}
			        		if(_this.debug){
				        		console.log("topPositon: ", topPositon);
				        		console.log("leftPositon: ", leftPositon);
				        	}
			        		position[0] = topPositon;
			        		position[1] = leftPositon;
			        		lastPostion.push(position);
			        	} else {
			        		position[1] = landigLeft * (_this.rowWidth + _this.gap); 
			        	}
		        	
		        	} else {
		        		// Ustaw według standardowego położenia

			        	// USTAW odstęp od lewej
			        	position[1] = landigLeft * (_this.rowWidth + _this.gap); 

			        	// USTAW odstęp od góry
			        	// NIe ustawiaj pierwszego rzędu
			        	if(index>_this.count-1) {
			        		var brotherTop = children.filter(":visible").eq(index-_this.count);
			        		position[0] = memorycalc[index-_this.count].top + brotherTop.outerHeight(true);
			        	} else {
			        		// console.log("N"+index);
			        	}
		        	}


		        	memorycalc[index] = {left: position[1], top: position[0]};


					// Następny rząd
					landigLeft += 1;
					if(landigLeft>=_this.count){
						landigLeft = 0;
					}



					// Ustaw koniec girda
		        	if(memorycalc[index].top + element.outerHeight(true) > _this.getBest){
		        		_this.getBest = memorycalc[index].top + element.outerHeight(true);
		        	}
		        	// console.log("getBest: ", _this.getBest);


		        }); 
		        j_this.css('height',_this.getBest+'px');


				children.each(function() {

		        	var element = $(this),
		        		index = element.filter(":visible").index();

		        	var elementMemoru = memorycalc[index];




		        	// Nadaj pozycje css
		        	if(elementMemoru){
						element.css({
							position: 'absolute',
							top: elementMemoru.top+'px',
							left: elementMemoru.left+'px'
						});
					}

		        }); 
		        $(_this.child).css('transition', '');
			}


			_this.init();
			// $(_this.child).css('transition', 'none');
			// setTimeout(function() {
			// 	$(_this.child).css('transition', '');
			// }, 1000);
			if(_this.interval){
				setInterval(function(){ 
					_this.init();
				}, _this.interval);
			}
			setTimeout(function() {
				_this.init();
			}, 500);


			_this.off = function(){

				j_this.css({
					'column-count': '',
					'column-gap': '',
					'position': '',
					'min-height': ''
				});
				_this.child.each(function() {
		        	var element = $(this);

					element.css({
						width: '',
						position: '',
						top: '',
						left: ''
					});
		        }); 
			}
			$( window ).resize(function() {
				if(_this.debug){
					console.log("window!!!");
				}
				if(_this.bodywidth != $(window).width()){
					$(_this.child).css('transition', 'none');
					_this.bodywidth = $(window).width();
					_this.init();
				}
			});
			// $(_this.child).resize(function() {

			// 	// $(_this.child).css('transition', 'none');
			// 	console.log("elment!!!");
			// 	_this.init();
			// });


			// if(_this.interval){
			// 	$(_this.child).off('transitionend webkitTransitionEnd oTransitionEnd');
			// } else {
			// 	$(_this.child).on('transitionend webkitTransitionEnd oTransitionEnd', function () {
			// 		// $(_this.child).css('transition', 'none');
			// 		_this.init();
			// 	});
			// 	$(window).bind("load", function() {
			// 		$(_this.child).css('transition', 'none');
			// 		_this.init();

			// 	});
			// }




		}); 
		return _this
    }
    // $('[data-columnstojs]').each(function() {
    // 	var el = $(this);
    // 	var json = el.data('columnstojs');
    // 	json = json.replace(/'/g , '"');
    // 	console.log(json);
    // 	el.columnsToJs(JSON.parse(json));
    // });
})(jQuery);